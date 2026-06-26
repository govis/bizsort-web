import { LitElement, html, css } from 'lit';
import { customElement, property, state, query } from 'lit/decorators.js';
import { IdName, Autocomplete } from '../../../model/foundation';
import { autocomplete as fetchLocations } from '../../../service/location';
import { Loader } from '@googlemaps/js-api-loader';

import '@awesome.me/webawesome/dist/components/input/input.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import type WaInput from '@awesome.me/webawesome/dist/components/input/input.js';
import { Input as LocationInputViewModel } from '../../../viewmodel/location/input';
import { IViewAdapter } from '../../../viewmodel';
import type WaDropdown from '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';

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
        this._suggestions = [];
        this._isDropdownOpen = false;
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
            this._errorText = this.model.validateable.errorInfo.getError('self') || '';
        }
    }
    static styles = css`
        :host {
            display: block;
            width: 100%;
        }

        wa-dropdown {
            width: 100%;
        }

        wa-menu {
            max-height: 300px;
            overflow-y: auto;
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
    declare _suggestions: any[];

    @state()
    declare _isDropdownOpen: boolean;

    @state()
    declare _errorText: string;

    @query('wa-input')
    private inputElement!: WaInput;

    private _debounceTimer: number | null = null;
    private _googleAutocomplete: google.maps.places.Autocomplete | null = null;

    protected firstUpdated() {
        if (this.geoMode) {
            this.initGooglePlaces();
        }
    }

    updated(changedProperties: Map<string, any>) {
        if (changedProperties.has('geoMode')) {
            if (this.geoMode && !this._googleAutocomplete) {
                this.initGooglePlaces();
            }
        }
    }

    private async initGooglePlaces() {
        const apiKey = process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY || '';
        if (!apiKey) return;

        try {
            const loader = new Loader({
                apiKey,
                version: "weekly",
                libraries: ["places"]
            });

            await (loader as any).importLibrary('places');
            
            // Wait for the wa-input to expose its internal input
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
        this._text = input.value || '';
        this.selected = null;
        this._errorText = '';

        if (this.geoMode) {
            // Google Places API handles dropdown and suggestions natively via DOM manipulation
            return;
        }

        if (this._debounceTimer) clearTimeout(this._debounceTimer);
        
        if (this._text.trim().length > 0) {
            this._debounceTimer = window.setTimeout(() => this.fetchSuggestions(), 300);
        } else {
            this._suggestions = [];
            this._isDropdownOpen = false;
        }
    }

    private async fetchSuggestions() {
        if (this.geoMode) return;
        
        try {
            const results = await fetchLocations(this.scope.id, this._text, this.scope);
            this._suggestions = results || [];
            this._isDropdownOpen = this._suggestions.length > 0;
        } catch (err) {
            console.error('Failed to fetch location suggestions', err);
            this._suggestions = [];
            this._isDropdownOpen = false;
        }
    }

    private handleSelect(e: CustomEvent) {
        const item = e.detail.item;
        const id = Number(item.value);
        const suggestion = this._suggestions.find(s => s.id === id);
        
        if (suggestion) {
            this.selected = { id: suggestion.id, name: suggestion.name };
            this._text = suggestion.name;
            this._suggestions = [];
            this._isDropdownOpen = false;
            this._errorText = '';
            
            this.dispatchEvent(new CustomEvent('location-selected', {
                detail: this.selected,
                bubbles: true,
                composed: true
            }));
        }
    }

    private handleClear(e: Event) {
        e.stopPropagation();
        this.selected = null;
        this._text = '';
        this._suggestions = [];
        this._isDropdownOpen = false;
        if (this.inputElement) {
            this.inputElement.value = '';
            this.inputElement.focus();
        }
        
        this.dispatchEvent(new CustomEvent('location-cleared', {
            bubbles: true,
            composed: true
        }));
    }

    private handleHide() {
        this._isDropdownOpen = false;
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
        const inputTemplate = html`
            <wa-input
                slot="trigger"
                exportparts="base, input, form-control-label"
                placeholder=${this.placeholder}
                label=${this.label}
                .value=${this.selected ? this.selected.name : this._text}
                @wa-input=${this.handleInput}
                ?readonly=${!!this.selected && !this.geoMode}
                help-text=${this._errorText}
                ?invalid=${!!this._errorText}
            >
                ${this.selected ? html`
                    <div slot="prefix" class="selected-container">
                        <wa-button 
                            variant="default"
                            is-icon-button
                            @click=${this.handleClear}
                            title="Clear selection">
                            <wa-icon name="x" library="system"></wa-icon>
                        </wa-button>
                    </div>
                ` : html`
                    <wa-icon slot="prefix" name="map" library="system"></wa-icon>
                `}
            </wa-input>
        `;

        if (this.geoMode) {
            // Google Places API will attach its own dropdown to the input
            return inputTemplate;
        }

        return html`
            <wa-dropdown 
                .open=${this._isDropdownOpen} 
                @wa-hide=${this.handleHide}
                stay-open-on-select>
                
                ${inputTemplate}

                <div @wa-select=${this.handleSelect}>
                    ${this._suggestions.map(s => html`
                        <wa-dropdown-item value="${s.id}">
                            ${s.name}
                            ${s.path ? html`<span class="path-text">in ${s.path.join(', ')}</span>` : ''}
                        </wa-dropdown-item>
                    `)}
                </div>
            </wa-dropdown>
        `;
    }
}
