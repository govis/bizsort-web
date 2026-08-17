import { LitElement, html, css } from 'lit';
import { property, state } from 'lit/decorators.js';
import { ListPagerViewModel } from '../../viewmodel/list/pager';
import type { IViewAdapter } from '../../viewmodel';

import '@awesome.me/webawesome/dist/components/button/button.js';

import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/select/select.js';
import '@awesome.me/webawesome/dist/components/option/option.js';

export class ListPager extends LitElement implements IViewAdapter {
    @property({ type: Object }) declare master: any;
    @property({ type: Boolean, reflect: true }) declare hidden: boolean;
    
    viewModel: ListPagerViewModel;
    
    @state() declare _pageButtons: any[];
    @state() declare _previousPage: any;
    @state() declare _nextPage: any;

    constructor() {
        super();
        this.hidden = true;
        this._pageButtons = [];
        this._previousPage = {};
        this._nextPage = {};
        this.viewModel = new ListPagerViewModel(this);
    }

    connectedCallback() {
        super.connectedCallback();
        if (this.master) {
            (this.viewModel as any).initialize({ master: this.master });
        }
    }
    
    willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
        if (changedProperties.has('master') && this.master) {
            (this.viewModel as any).initialize({ master: this.master });
        }
    }

    modelUpdated(props: string[]) {
        let update = false;
        props.forEach(prop => {
            switch (prop) {
                case 'pageButtons':
                    this._pageButtons = [...(this.viewModel as any).pageButtons];
                    update = true;
                    break;
                case 'previousPage':
                    this._previousPage = { ...(this.viewModel as any).previousPage };
                    update = true;
                    break;
                case 'nextPage':
                    this._nextPage = { ...(this.viewModel as any).nextPage };
                    update = true;
                    break;
                case 'pageCount':
                    this.hidden = !((this.viewModel as any).pageCount > 1);
                    break;
            }
        });
        if (update) this.requestUpdate();
    }

    private _handleAction(event: Event) {
        const target = event.currentTarget as HTMLElement;
        const action = target.dataset.action;
        const pager = (this.viewModel as any).master;
        
        if (!pager) return;

        switch (action) {
            case "Prev":
                if (this._previousPage.canMove && pager.pageIndex > 0) {
                    pager.moveToPreviousPage();
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
                break;
            case "Next":
                if (this._nextPage.canMove) {
                    pager.moveToNextPage();
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
                break;
            case "Page":
                const pageNum = Number(target.dataset.page);
                if (pageNum) {
                    const pageIndex = pageNum - 1;
                    if (pager.pageIndex != pageIndex) {
                        pager.moveToPage(pageIndex);
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                    }
                }
                break;
        }
        event.preventDefault();
        event.stopPropagation();
    }

    static styles = css`
        :host {
            display: flex;
            flex-direction: row;
            align-items: center;
            justify-content: center;
            margin-top: 2rem;
            margin-bottom: 2rem;
            gap: 0.25rem;
        }
        
        :host([hidden]) {
            display: none !important;
        }

        wa-button[variant="brand"]:not([appearance="plain"]) {
            /* Ensure the selected state looks like a rounded square */
            --wa-border-radius-m: 4px;
        }

        .page-button {
            min-width: 40px;
        }
    `;

    render() {
        return html`
            <wa-button 
                variant="brand"
                appearance="plain"
                is-icon-button
                ?disabled="${!this._previousPage.canMove}" 
                @click="${this._handleAction}"
                data-action="Prev"
            >
                <wa-icon name="chevron-left" library="system"></wa-icon>
            </wa-button>
            
            ${this._pageButtons.map(page => html`
                <wa-button 
                    class="page-button"
                    variant="brand"
                    appearance=${page.selected ? 'filled' : 'plain'}
                    data-action="Page"
                    data-page=${page.pageNumber}
                    @click=${this._handleAction}
                >
                    ${page.pageNumber}
                </wa-button>
            `)}
            
            <wa-button 
                variant="brand"
                appearance="plain"
                is-icon-button
                ?disabled="${!this._nextPage.canMove}" 
                @click="${this._handleAction}"
                data-action="Next"
            >
                <wa-icon name="chevron-right" library="system"></wa-icon>
            </wa-button>
        `;
    }
}

if (!customElements.get('list-pager')) {
    customElements.define('list-pager', ListPager);
}

export class ListPageSelect extends LitElement {
    @property({ type: Object }) declare pager: any;
    
    private _unobserve: any;
    
    connectedCallback() {
        super.connectedCallback();
        if (this.pager && this.pager.observeProperty) {
            this._unobserve = this.pager.observeProperty((sender: any, prop: string) => {
                if (prop === 'pageSize' || prop === 'pageSizes') {
                    this.requestUpdate();
                }
            });
        }
    }
    
    disconnectedCallback() {
        super.disconnectedCallback();
        if (this._unobserve) this._unobserve();
    }

    static styles = css`
        wa-select {
            /* Color the text and icons primary blue to match pager */
            --wa-form-control-value-color: var(--wa-color-primary-500, #4285f4);
            --wa-color-neutral-on-quiet: var(--wa-color-primary-500, #4285f4); /* Select caret icon */
            
            /* Backgrounds */
            --wa-form-control-background-color: transparent;
            
            /* Flat Underline Borders (Blue) */
            --wa-form-control-border-color: var(--wa-color-primary-500, #4285f4);
            --wa-form-control-border-width: 0 0 1px 0;
            --wa-form-control-border-radius: 0;
            
            /* Disable Default Focus Ring */
            --wa-focus-ring-width: 0;
        }

        wa-select:focus-within {
            --wa-form-control-border-width: 0 0 2px 0;
        }
    `;

    render() {
        if (!this.pager || !this.pager.pageSizes || this.pager.pageSizes.length === 0) return html``;
        
        return html`
            <wa-select size="medium" .value=${this.pager.pageSize.toString()} @change=${(e: any) => { 
                const newSize = Number(e.target.value);
                console.log('[ListPageSelect] Dropdown changed. Setting new page size:', newSize);
                this.pager.pageSize = newSize; 
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }}>
                ${this.pager.pageSizes.map((size: number) => html`<wa-option value="${size}">${size} per page</wa-option>`)}
            </wa-select>
        `;
    }
}

if (!customElements.get('list-page-select')) {
    customElements.define('list-page-select', ListPageSelect);
}
