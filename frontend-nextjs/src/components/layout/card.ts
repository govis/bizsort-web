import { LitElement, html, css } from 'lit';
import '@awesome.me/webawesome/dist/components/card/card.js';

export class LayoutCard extends LitElement {
  static get properties() {
    return {
      heading: { type: String },
      noPadding: { type: Boolean, attribute: 'no-padding' }
    };
  }

  declare heading?: string;
  declare noPadding: boolean;

  constructor() {
    super();
    this.noPadding = false;
  }

  static styles = css`
    :host { display: block; }
    .card {
      width: 100%;
      border-radius: 2px;
      --border-color: var(--wa-color-neutral-200);
      --border-radius: 2px;
      box-shadow: 0 2px 2px 0 rgba(0,0,0,0.14), 0 1px 5px 0 rgba(0,0,0,0.12), 0 3px 1px -2px rgba(0,0,0,0.2);
      background-color: white;
      margin-bottom: 1.5rem;
      overflow: hidden;
    }
    .card::part(header) {
      font-size: 20px;
      font-weight: 400;
      color: #4285f4;
      border-bottom: 1px solid #e0e0e0;
      padding: 1rem 1.5rem;
    }
    .card::part(body) {
      padding: 1.5rem;
      line-height: 1.6;
      color: #333;
    }
    :host([no-padding]) .card::part(body) {
      padding: 0;
    }
  `;

  render() {
    return html`
      <wa-card class="card">
        ${this.heading ? html`<div slot="header">${this.heading}</div>` : ''}
        <slot></slot>
      </wa-card>
    `;
  }
}

if (!customElements.get('layout-card')) {
  customElements.define('layout-card', LayoutCard);
}
