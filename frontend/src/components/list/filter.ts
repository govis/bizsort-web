import { LitElement, html, css } from 'lit';
import { property, state } from 'lit/decorators.js';
import { ListFilterAvailableViewModel, ListFilterAppliedViewModel } from '../../viewmodel/list/filter';
import type { IViewAdapter } from '../../viewmodel';
import type { Semantic } from '../../viewmodel/list/filter';


import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/tag/tag.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

export class ListFilterAvailable extends LitElement implements IViewAdapter {
    @property({ type: Boolean, reflect: true }) declare hidden: boolean;
    
    viewModel: ListFilterAvailableViewModel;
    
    @state() declare _facets: Semantic.FacetName[];

    constructor() {
        super();
        this.hidden = true;
        this._facets = [];
        this.viewModel = new ListFilterAvailableViewModel(this);
    }

    modelUpdated(props: string[]) {
        if (props.includes('facets')) {
            this._facets = [...this.viewModel.facets];
            this.hidden = this._facets.length === 0;
            this.requestUpdate();
        }
    }

    static styles = css`
        :host {
            display: inline-block;
        }
        :host([hidden]) {
            display: none !important;
        }
        .facet-header {
            padding: 8px 16px;
            font-size: 0.85rem;
            font-weight: bold;
            color: var(--wa-color-neutral-600);
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        wa-dropdown-item {
            font-size: 0.95rem;
        }
        .exclude-btn {
            opacity: 0.5;
            transition: opacity 0.2s;
        }
        .exclude-btn:hover {
            opacity: 1;
            color: var(--wa-color-danger-600);
        }
    `;

    render() {
        return html`
            <wa-dropdown placement="bottom-start" hoist>
                <wa-button slot="trigger" variant="text" is-icon-button>
                    <wa-icon name="filter-list" library="system"></wa-icon>
                </wa-button>
                    ${this._facets.map(item => html`
                        <div class="facet-header">${item.text}</div>
                        ${item.values.map(value => html`
                            <wa-dropdown-item value="${value.key}" @click=${() => this.viewModel.filterIn(value)}>
                                ${value.text} (${value.count})
                                <wa-button 
                                    slot="suffix" 
                                    variant="text"
                                    class="exclude-btn" 
                                    is-icon-button
                                    @click=${(e: Event) => { e.stopPropagation(); this.viewModel.filterOut(value); }}
                                >
                                    <wa-icon name="x" library="system"></wa-icon>
                                </wa-button>
                            </wa-dropdown-item>
                        `)}
                        <wa-divider></wa-divider>
                    `)}
            </wa-dropdown>
        `;
    }
}

if (!customElements.get('list-filter-available')) {
    customElements.define('list-filter-available', ListFilterAvailable);
}

export class ListFilterApplied extends LitElement implements IViewAdapter {
    viewModel: ListFilterAppliedViewModel;
    
    @state() declare _facets: Semantic.Facet[];

    constructor() {
        super();
        this._facets = [];
        this.viewModel = new ListFilterAppliedViewModel(this);
    }

    modelUpdated(props: string[]) {
        if (props.includes('facets')) {
            this._facets = [...this.viewModel.facets];
            this.requestUpdate();
        }
    }

    static styles = css`
        :host {
            display: flex;
            flex-direction: row;
            align-items: center;
            flex-wrap: wrap;
            gap: 8px;
        }
    `;

    render() {
        return html`
            ${this._facets.map(item => html`
                <wa-tag 
                    variant=${item.exclude ? 'danger' : 'brand'} 
                    with-remove 
                    @wa-remove=${() => this.viewModel.onFilterSelected(item)}
                >
                    ${item.exclude ? 'NOT ' : ''}${item.valueText}
                </wa-tag>
            `)}
        `;
    }
}

if (!customElements.get('list-filter-applied')) {
    customElements.define('list-filter-applied', ListFilterApplied);
}
