import { LitElement, html, css } from 'lit';
import { property, state } from 'lit/decorators.js';
import { ListViewModel, ViewType, ItemOption } from '../../viewmodel/list/listview';
import type { IViewAdapter } from '../../viewmodel';

import './card';
// Note: company-listitem component to be ported or mapped to a horizontal card

export class CompanyListView extends LitElement implements IViewAdapter {
    @property({ type: Boolean, attribute: 'list' })
    declare _list: boolean;

    @property({ type: Boolean })
    declare noCategory: boolean;

    @property({ type: Array })
    declare items: any[]; // Explicitly binding items from host for reactive propagation

    @state() declare _items: any[];
    @state() declare _displayOptions: any;
    
    viewModel: ListViewModel;

    constructor() {
        super();
        this._list = false;
        this.noCategory = false;
        this.items = [];
        this._items = [];
        this._displayOptions = {};
        this.viewModel = new ListViewModel(this);
    }

    connectedCallback() {
        super.connectedCallback();
        this.viewModel.initialize();
        if (this._list) {
            this.viewModel.type = ViewType.List;
        }
        if (!this.noCategory) {
            this.viewModel.setItemOption('category', ItemOption.FetchOptIn);
        }
    }

    willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
        if (changedProperties.has('items')) {
            this.viewModel.items = this.items;
        }
    }

    modelUpdated(props: string[]) {
        if (props.includes('items')) {
            this._items = [...this.viewModel.items];
        }
        if (props.includes('displayOptions')) {
            this._displayOptions = { ...this.viewModel.displayOptions };
        }
        this.requestUpdate();
    }

    static styles = css`
        :host {
            display: block;
            width: 100%;
        }
        :host([list]) {
            max-width: 800px;
            margin-left: auto;
            margin-right: auto;
        }
        .grid-view {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
            gap: 1.5rem;
            justify-items: center;
        }
        .list-view {
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }

        @keyframes list-enter {
            from {
                opacity: 0;
                transform: translateY(40px) scale(0.95);
            }
            to {
                opacity: 1;
                transform: translateY(0) scale(1);
            }
        }

        company-card {
            animation: list-enter 500ms cubic-bezier(0.4, 0, 0.2, 1) both;
        }

        /* 10-item cascade to mimic legacy JS index * delay without massive infinite wait times */
        company-card:nth-child(10n + 1) { animation-delay: 0ms; }
        company-card:nth-child(10n + 2) { animation-delay: 60ms; }
        company-card:nth-child(10n + 3) { animation-delay: 120ms; }
        company-card:nth-child(10n + 4) { animation-delay: 180ms; }
        company-card:nth-child(10n + 5) { animation-delay: 240ms; }
        company-card:nth-child(10n + 6) { animation-delay: 300ms; }
        company-card:nth-child(10n + 7) { animation-delay: 360ms; }
        company-card:nth-child(10n + 8) { animation-delay: 420ms; }
        company-card:nth-child(10n + 9) { animation-delay: 480ms; }
        company-card:nth-child(10n + 10) { animation-delay: 540ms; }
    `;

    render() {
        if (!this._items || this._items.length === 0) {
            return html``;
        }

        return html`
            ${this.viewModel.type !== ViewType.List ? html`
                <div class="grid-view">
                    ${this._items.map(item => html`
                        <company-card 
                            .model=${item} 
                            .itemOptions=${this._displayOptions}
                        ></company-card>
                    `)}
                </div>
            ` : html`
                <div class="list-view">
                    ${this._items.map(item => html`
                        <!-- Temporary fallback to card until company-listitem is fully ported -->
                        <company-card 
                            .model=${item} 
                            .itemOptions=${this._displayOptions}
                            layout="horizontal"
                        ></company-card>
                    `)}
                </div>
            `}
        `;
    }
}

if (!customElements.get('company-listview')) {
    customElements.define('company-listview', CompanyListView);
}
