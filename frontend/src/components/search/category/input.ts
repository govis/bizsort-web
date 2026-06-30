import { LitElement, html, css } from 'lit';
import { customElement, property, state, query } from 'lit/decorators.js';
import { IdName, Autocomplete } from '../../../model/foundation';
import { autocomplete as fetchCategories } from '../../../service/category';

import '@awesome.me/webawesome/dist/components/input/input.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/popup/popup.js';
import type WaInput from '@awesome.me/webawesome/dist/components/input/input.js';
import { Input as CategoryInputViewModel } from '../../../viewmodel/search/category/input';
import { IViewAdapter } from '../../../viewmodel';
import type WaDropdown from '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '../../group/autocomplete';

@customElement('search-category-input')
export class SearchCategoryInput extends LitElement implements IViewAdapter {
    model: CategoryInputViewModel;
    
    constructor() {
        super();
        this.model = new CategoryInputViewModel(this);
    
        this.scope = { id: 0, name: 'All Categories' };
        this.placeholder = 'Search categories...';
        this.label = '';
        this.selected = null;
        this._text = '';
        this._errorText = '';
    }
    
    modelUpdated(props: string[]) {
        if (props.includes('text')) {
            this._text = this.model.text;
        }
        if (props.includes('selected')) {
            this.selected = this.model.selected;
        }
        if (props.includes('errorInfo')) {
            this._errorText = this.model.validateable.errorInfo.getError('self') || '';
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

        .selected-text {
            flex-grow: 1;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            padding-right: 8px;
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
    declare selected: IdName | null;

    @state()
    declare _text: string;

    @state()
    declare _errorText: string;

    @query('wa-input')
    private inputElement!: WaInput;

    private _debounceTimer: number | null = null;

    private handleInput(e: Event) {
        const input = e.target as WaInput;
        const text = input.value || '';
        this._errorText = '';

        if (this._debounceTimer) clearTimeout(this._debounceTimer);
        
        this._debounceTimer = window.setTimeout(() => {
            this.model.text = text;
            if (this.model.autocomplete) {
                this.model.autocomplete.active = text.trim().length > 0;
            }
        }, 300);
    }

    private handleClear(e: Event) {
        e.stopPropagation();
        this.selected = null;
        this.model.text = '';
        if (this.model.autocomplete) {
            this.model.autocomplete.active = false;
        }
        if (this.inputElement) {
            this.inputElement.value = '';
            this.inputElement.focus();
        }
        
        this.dispatchEvent(new CustomEvent('category-cleared', {
            bubbles: true,
            composed: true
        }));
    }

    // Expose validation mechanism
    public validate(): boolean {
        if (!this.selected && !this._text.trim()) {
            this._errorText = 'Search criteria is required.';
            return false;
        }
        this._errorText = '';
        return true;
    }

    protected firstUpdated() {
        this.model.initialize();
        this.requestUpdate(); // Force re-render to pass this.inputElement to group-autocomplete
    }

    render() {
        return html`
            <group-autocomplete .model=${this.model.autocomplete}>
                <wa-input
                    id="search-input"
                    exportparts="base, input, form-control-label"
                    placeholder=${this.placeholder}
                    label=${this.label}
                    .value=${this._text}
                    @input=${this.handleInput}
                    @focus=${() => { if (this.model.autocomplete && this.model.autocomplete.items.length > 0) this.model.autocomplete.active = true; }}
                    help-text=${this._errorText}
                    ?invalid=${!!this._errorText}
                >
                    <wa-icon slot="prefix" name="search" library="system"></wa-icon>
                </wa-input>
            </group-autocomplete>

            ${this.selected && this.selected.id ? html`
                <div class="selected-chip" style="display: flex; align-items: center; gap: 8px; margin-top: 8px; font-size: 14px; color: var(--wa-color-primary-text);">
                    <span class="category-selected">${this.selected.name}</span>
                    <wa-button 
                        variant="default"
                        size="small"
                        is-icon-button
                        @click=${this.handleClear}
                        title="Clear selection">
                        <wa-icon name="x" library="system"></wa-icon>
                    </wa-button>
                </div>
            ` : ''}
        `;
    }
}
