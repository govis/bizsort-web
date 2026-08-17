import { LitElement, html, css } from 'lit';
import { property, state } from 'lit/decorators.js';
import { ListViewModel, ViewType, ItemOption } from '../../viewmodel/list/listview';
import type { IViewAdapter } from '../../viewmodel';

import './card';

export class ProductListView extends LitElement implements IViewAdapter {
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
    `;

    render() {
        if (!this._items || this._items.length === 0) {
            return html``;
        }

        return html`
            ${this.viewModel.type !== ViewType.List ? html`
                <div class="grid-view">
                    ${this._items.map(item => html`
                        <product-card 
                            .model=${item} 
                        ></product-card>
                    `)}
                </div>
            ` : html`
                <div class="list-view">
                    ${this._items.map(item => html`
                        <!-- Temporary fallback to card until listitem is fully ported -->
                        <product-card 
                            .model=${item} 
                            layout="horizontal"
                        ></product-card>
                    `)}
                </div>
            `}
        `;
    }
}

if (!customElements.get('product-listview')) {
    customElements.define('product-listview', ProductListView);
}
