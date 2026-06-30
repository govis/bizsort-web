import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { IdName, Autocomplete as AutocompleteModel } from '../../model/foundation';
import { Autocomplete as AutocompleteViewModel } from '../../viewmodel/group/autocomplete';
import { IViewAdapter } from '../../viewmodel';

import '@awesome.me/webawesome/dist/components/popup/popup.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

@customElement('group-autocomplete')
export class GroupAutocomplete extends LitElement implements IViewAdapter {
    static styles = css`
        :host {
            display: block;
            width: 100%;
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

        .path-text {
            font-size: 0.85em;
            color: var(--wa-color-neutral-text-subtle, #666);
            margin-left: 8px;
        }
    `;

    @property({ type: Object, attribute: false })
    declare model: AutocompleteViewModel;

    @property({ type: Array })
    declare items: AutocompleteModel[];

    @property({ type: Boolean })
    declare active: boolean;

    constructor() {
        super();
        this.items = [];
        this.active = false;
    }

    updated(changedProperties: Map<string, any>) {
        if (changedProperties.has('model') && this.model) {
            // Re-bind the model's view to this component so we receive notifyView events!
            this.model.view = this;
            
            // Sync initial state
            this.items = this.model.items || [];
            this.active = this.model.active;
        }
    }

    modelUpdated(props: string[]) {
        if (props.includes('items')) {
            this.items = this.model.items || [];
        }
        if (props.includes('active')) {
            this.active = this.model.active;
        }
        return true;
    }

    private selectItem(item: AutocompleteModel) {
        if (this.model.itemSelected) {
            this.model.itemSelected(item);
        }
        this.model.active = false;
    }

    render() {
        const isActive = this.active && this.items && this.items.length > 0;
        
        return html`
            <wa-popup
                placement="bottom-start"
                ?active=${isActive}
                sync="width"
                auto-size="vertical"
                auto-size-padding="10"
                flip
            >
                <slot slot="anchor"></slot>

                <div class="dropdown-panel">
                    ${this.items?.map(item => html`
                        <wa-dropdown-item @mousedown=${(e: Event) => { e.preventDefault(); this.selectItem(item); }}>
                            ${item.name}
                            ${item.path ? html`<span class="path-text">in ${item.path.join(', ')}</span>` : ''}
                        </wa-dropdown-item>
                    `)}
                </div>
            </wa-popup>
        `;
    }
}
