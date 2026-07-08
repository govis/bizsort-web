import { LitElement, html, css } from 'lit';
import { property } from 'lit/decorators.js';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';

export class RichtextView extends LitElement {
    static get styles() {
        return css`
            :host {
                display: block;
                line-height: 1.5;
            }

            p {
                margin: 0;
            }

            p:empty {
                display: none;
            }

            p:not(:empty) + p {
                margin-top: 12px;
            }
        `;
    }

    render() {
        return html`
            <div id="content">${this.html ? unsafeHTML(this.html) : ''}</div>
        `;
    }

    @property({ type: String })
    declare html?: string;

    shouldUpdate() {
        return !!this.html;
    }
}

if (!customElements.get('richtext-view')) {
    customElements.define('richtext-view', RichtextView);
}
