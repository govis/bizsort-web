/// <reference types="google.maps" />
import { LitElement, html, css, PropertyValues } from 'lit';
import { customElement, property, state, query } from 'lit/decorators.js';
import { IdName, Autocomplete } from '../../../model/foundation';
import { autocomplete as fetchLocations } from '../../../service/location';
import { setOptions, importLibrary } from '@googlemaps/js-api-loader';

import '@awesome.me/webawesome/dist/components/input/input.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/popup/popup.js';
import type WaInput from '@awesome.me/webawesome/dist/components/input/input.js';
import { Input as LocationInputViewModel } from '../../../viewmodel/search/location/input';
import { IViewAdapter } from '../../../viewmodel';
import type WaDropdown from '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '../../group/autocomplete';

@customElement('search-location-input')
export class SearchLocationInput extends LitElement implements IViewAdapter {
    model: LocationInputViewModel;
    
    constructor() {
        super();
        this.model = new LocationInputViewModel(this);
    
        this.scope = { id: 0, name: 'Everywhere' };
        this.placeholder = 'Search locations...';
        this.label = '';
        this.selected = null;
        this.geoMode = false;
        this._text = '';
        this._errorText = '';
    }
    
    modelUpdated(props: string[]) {
        if (props.includes('text')) {
            this._text = this.model.text;
        }
        if (props.includes('geoLocation')) {
            this.selected = this.model.geoLocation;
        }
        if (props.includes('errorInfo')) {
            this._errorText = this.model.validateable.errorInfo.getError('locationInput') || this.model.validateable.errorInfo.getError('self') || '';
        }
        if (props.includes('geoMode') && this.geoMode !== this.model.geoMode) {
            this.geoMode = this.model.geoMode;
            this.dispatchEvent(new CustomEvent('geomodeChange', {
                composed: true, detail: { value: this.geoMode }
            }));
        }
    }

    updated(changedProperties: Map<string, any>) {
        super.updated(changedProperties);
        if (changedProperties.has('geoMode') && this.geoMode !== this.model.geoMode) {
            this.model.geoMode = this.geoMode;
        }
        if (changedProperties.has('_errorText') && this.inputElement) {
            this.inputElement.setCustomValidity(this._errorText || '');
        }
        if (changedProperties.has('geoMode')) {
            if (this.geoMode && !this._googleAutocomplete) {
                this.initGooglePlaces();
            }
        }
    }
    static styles = css`
        :host {
            display: block;
            width: 100%;
            position: relative;
        }

        .dropdown-panel {
            width: 100%;
            max-height: 300px;
            overflow-y: auto;
            background-color: var(--wa-color-surface-raised, #fff);
            border: 1px solid var(--wa-color-surface-border, #ddd);
            border-radius: var(--wa-border-radius-m, 4px);
            box-shadow: var(--wa-shadow-m, 0 4px 6px rgba(0,0,0,0.1));
        }

        wa-popup {
            width: 100%;
            --z-index: 9999;
        }

        .selected-container {
            display: flex;
            align-items: center;
            padding: 0;
        }

        .path-text {
            font-size: 0.85em;
            color: var(--wa-color-neutral-text-subtle, #666);
            margin-left: 8px;
        }

        .selected-container wa-button {
            margin-left: -4px;
        }

        wa-input {
            /* Backgrounds (covers outlined and filled appearances) */
            --wa-form-control-background-color: transparent;
            --wa-color-neutral-fill-quiet: transparent;
            
            /* Text & Placeholders */
            --wa-form-control-value-color: var(--search-home-text-color, var(--text-color-on-primary));
            --wa-form-control-placeholder-color: var(--search-home-text-color, var(--text-color-on-primary));
            --wa-color-neutral-on-quiet: var(--search-home-text-color, var(--text-color-on-primary)); /* Prefix icons */
            
            /* Flat Underline Borders */
            --wa-form-control-border-color: var(--search-home-text-color, var(--text-color-on-primary));
            --wa-form-control-border-width: 0 0 1px 0;
            --wa-form-control-border-radius: 0;
            
            /* Disable Default Focus Ring */
            --wa-focus-ring-width: 0;
        }

        wa-input:focus-within {
            --wa-form-control-border-width: 0 0 2px 0;
        }
    `;

    @property({ type: Object })
    declare scope: IdName;

    @property({ type: String })
    declare placeholder: string;

    @property({ type: String })
    declare label: string;

    @property({ type: Object })
    declare selected: any | null;

    @property({ type: Boolean })
    declare geoMode: boolean;

    @state()
    declare _text: string;

    @state()
    declare _errorText: string;

    @query('wa-input')
    private declare inputElement: WaInput;

    private _debounceTimer: number | null = null;
    private _googleAutocomplete: google.maps.places.Autocomplete | null = null;

    protected firstUpdated() {
        if (!this.model.initialized) {
            this.model.initialize();
        }
        if (this.geoMode) {
            this.initGooglePlaces();
        }
        this.requestUpdate();
    }



    private async initGooglePlaces() {
        const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY || '';
        if (!apiKey) return;

        try {
            if (!(window as any).__googleMapsLoaderOptionsSet) {
                setOptions({
                    key: apiKey,
                    v: "weekly"
                });
                (window as any).__googleMapsLoaderOptionsSet = true;
            }

            await importLibrary('places');
            
            await this.updateComplete;
            const inputNative = this.inputElement.shadowRoot?.querySelector('input');
            
            if (inputNative) {
                this._googleAutocomplete = new google.maps.places.Autocomplete(inputNative, {
                    types: ['geocode']
                });

                this._googleAutocomplete.addListener('place_changed', () => {
                    const place = this._googleAutocomplete?.getPlace();
                    if (place && place.geometry) {
                        this.selected = {
                            id: 0, 
                            name: place.formatted_address || place.name,
                            geometry: place.geometry.location?.toJSON()
                        };
                        this._text = this.selected.name;
                        this._errorText = '';
                        this.model.geoInput.geoValidated = this.selected; // Pass payload to ViewModel
                        this.dispatchEvent(new CustomEvent('location-selected', {
                            detail: this.selected,
                            bubbles: true,
                            composed: true
                        }));
                    }
                });
            }
        } catch (e) {
            console.error('Error loading Google Maps API', e);
        }
    }

    private handleInput(e: Event) {
        const input = e.target as WaInput;
        const text = input.value || '';
        this._errorText = '';

        if (this.geoMode) {
            // Google Places API handles dropdown natively
            return;
        }

        if (this._debounceTimer) clearTimeout(this._debounceTimer);
        
        this._debounceTimer = window.setTimeout(() => {
            this.model.text = text;
            if (this.model.autocomplete) {
                this.model.autocomplete.active = text.trim().length > 0;
            }
        }, 300);
    }

    public validate(): boolean {
        if (!this.selected && !this._text.trim()) {
            this._errorText = 'Location is required.';
            return false;
        }
        this._errorText = '';
        return true;
    }

    render() {
        return html`
            <group-autocomplete .model=${this.model.autocomplete}>
                <wa-input
                    id="search-input"
                    exportparts="base, input, form-control-label"
                    placeholder=${this.placeholder}
                    label=${this.label}
                    .value=${this.selected && this.selected.id ? this.selected.name : this._text}
                    @input=${this.handleInput}
                    @focus=${() => { if (!this.geoMode && this.model.autocomplete && this.model.autocomplete.items.length > 0) this.model.autocomplete.active = true; }}
                    @blur=${() => { if (!this.geoMode && this.model.autocomplete) this.model.autocomplete.active = false; }}
                >
                    <wa-icon slot="prefix" name="geo-alt" library="system"></wa-icon>
                </wa-input>
            </group-autocomplete>
        `;
    }
}
