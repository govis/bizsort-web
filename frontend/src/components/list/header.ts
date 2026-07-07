import { LitElement, html, css } from 'lit';
import { property } from 'lit/decorators.js';

export class ListHeader extends LitElement {
    @property({ type: String }) declare entity: string;
    
    // The View engine sets this data object whenever the pager or filter updates
    @property({ type: Object }) declare data: any;

    constructor() {
        super();
        this.entity = 'records';
        this.data = null;
    }

    static styles = css`
        :host {
            font-size: 0.9rem;
            color: var(--wa-color-neutral-600, #666);
            padding: 8px 0;
            display: flex;
            align-items: center;
            min-height: 40px;
        }

        .empty-state {
            font-size: 1rem;
        }
        .empty-query {
            font-weight: 600;
            color: var(--wa-color-neutral-800, #333);
        }
    `;

    render() {
        if (!this.data) return html``;

        if (this.data.isEmpty) {
            if (this.data.query) {
                return html`
                    <span class="empty-state">
                        Your search for <span class="empty-query">"${this.data.query}"</span> did not return any results. 
                        You can try expanding the category or location scope of your search.
                    </span>
                `;
            } else {
                return html`<span>No ${this.entity} found.</span>`;
            }
        }

        return html`
            <span>
                Showing ${this.data.fromRecord} to ${this.data.toRecord} of ${this.data.totalCount} ${this.entity}
                ${this.data.query ? html` for <span class="empty-query">"${this.data.query}"</span>` : ''}
            </span>
        `;
    }
}

if (!customElements.get('list-header')) {
    customElements.define('list-header', ListHeader);
}
