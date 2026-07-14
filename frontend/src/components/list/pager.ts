import { LitElement, html, css } from 'lit';
import { property, state } from 'lit/decorators.js';
import { ListPagerViewModel } from '../../viewmodel/list/pager';
import type { IViewAdapter } from '../../viewmodel';

import '@awesome.me/webawesome/dist/components/button/button.js';

import '@awesome.me/webawesome/dist/components/icon/icon.js';

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
                if (this._previousPage.canMove && pager.pageIndex > 0)
                    pager.moveToPreviousPage();
                break;
            case "Next":
                if (this._nextPage.canMove)
                    pager.moveToNextPage();
                break;
            case "Page":
                const pageNum = Number(target.dataset.page);
                if (pageNum) {
                    const pageIndex = pageNum - 1;
                    if (pager.pageIndex != pageIndex)
                        pager.moveToPage(pageIndex);
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
            gap: 0.5rem;
        }
        
        :host([hidden]) {
            display: none !important;
        }
        
        .page-button {
            min-width: 40px;
        }
    `;

    render() {
        return html`
            <wa-button 
                variant="text" 
                is-icon-button
                ?disabled="${!(this.viewModel as any).canChangePage || (this.viewModel as any).pageIndex <= 0}" 
                @click="${(this.viewModel as any).moveToPreviousPage}"
            >
                <wa-icon name="chevron-left" library="system"></wa-icon>
            </wa-button>
            
            ${this._pageButtons.map(page => html`
                <wa-button 
                    class="page-button"
                    variant=${page.selected ? 'brand' : 'neutral'}
                    data-action="Page"
                    data-page=${page.pageNumber}
                    @click=${this._handleAction}
                >
                    ${page.pageNumber}
                </wa-button>
            `)}
            
            <wa-button 
                variant="text" 
                is-icon-button
                ?disabled="${!(this.viewModel as any).canChangePage || !(this.viewModel as any).hasPage((this.viewModel as any).pageIndex + 1)}" 
                @click="${() => (this.viewModel as any).moveToNextPage()}"
            >
                <wa-icon name="chevron-right" library="system"></wa-icon>
            </wa-button>
        `;
    }
}

if (!customElements.get('list-pager')) {
    customElements.define('list-pager', ListPager);
}
