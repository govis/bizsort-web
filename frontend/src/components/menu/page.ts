import { LitElement, html, css } from 'lit';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

export class PageMenu extends LitElement {
  static styles = css`
    :host { display: inline-block; }
    wa-button::part(base) {
      color: var(--wa-color-neutral-600);
    }
    :host([theme="dark"]) wa-button::part(base) {
      color: white;
    }
  `;

  static get properties() {
    return {
      theme: { type: String }
    };
  }

  declare theme?: string;

  render() {
    return html`
      <wa-dropdown placement="bottom-end">
        <wa-button slot="trigger" variant="text" is-icon-button>
          <wa-icon name="three-dots-vertical"></wa-icon>
        </wa-button>
        <slot></slot>
      </wa-dropdown>
    `;
  }
}

if (!customElements.get('page-menu')) {
  customElements.define('page-menu', PageMenu);
}
