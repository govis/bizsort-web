import { LitElement, html, css } from 'lit';
import { customElement, property, state, query } from 'lit/decorators.js';
import { IdName, Autocomplete } from '../../../model/foundation';
import { autocomplete as fetchCategories } from '../../../service/category';

import '@awesome.me/webawesome/dist/components/input/input.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import type WaInput from '@awesome.me/webawesome/dist/components/input/input.js';
import { Input as CategoryInputViewModel } from '../../../viewmodel/search/category/input';
import { IViewAdapter } from '../../../viewmodel';
import type WaDropdown from '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';

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
        this._suggestions = [];
        this._isDropdownOpen = false;
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
    declare _suggestions: Autocomplete[];

    @state()
    declare _isDropdownOpen: boolean;

    @state()
    declare _errorText: string;

    @query('wa-input')
    private inputElement!: WaInput;

    @query('wa-dropdown')
    private dropdownElement!: WaDropdown;

    private _debounceTimer: number | null = null;

    private handleInput(e: Event) {
        const input = e.target as WaInput;
        this._text = input.value || '';
        this.selected = null; // Clear selection if user types
        this._errorText = '';

        if (this._debounceTimer) clearTimeout(this._debounceTimer);
        
        if (this._text.trim().length > 0) {
            this._debounceTimer = window.setTimeout(() => this.fetchSuggestions(), 300);
        } else {
            this._suggestions = [];
            this._isDropdownOpen = false;
        }
    }

    private async fetchSuggestions() {
        try {
            const results = await fetchCategories(this.scope.id, this._text, this.scope);
            this._suggestions = results || [];
            this._isDropdownOpen = this._suggestions.length > 0;
        } catch (err) {
            console.error('Failed to fetch category suggestions', err);
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
            
            this.dispatchEvent(new CustomEvent('category-selected', {
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
        
        this.dispatchEvent(new CustomEvent('category-cleared', {
            bubbles: true,
            composed: true
        }));
    }

    private handleHide() {
        this._isDropdownOpen = false;
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

    render() {
        return html`
            <wa-dropdown 
                .open=${this._isDropdownOpen} 
                @wa-hide=${this.handleHide}
                stay-open-on-select>
                
                <wa-input
                    slot="trigger"
                    exportparts="base, input, form-control-label"
                    placeholder=${this.placeholder}
                    label=${this.label}
                    .value=${this.selected ? this.selected.name : this._text}
                    @wa-input=${this.handleInput}
                    ?readonly=${!!this.selected}
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
                        <wa-icon slot="prefix" name="search" library="system"></wa-icon>
                    `}
                </wa-input>

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
