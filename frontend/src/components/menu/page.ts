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
    wa-dropdown::part(panel) {
      background-color: white;
      border: none;
      border-radius: 4px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.2);
    }
    :host([theme="dark"]) wa-dropdown::part(panel) {
      background-color: var(--primary-theme-color, #448aff);
    }
    ::slotted(wa-dropdown-item) {
      color: var(--wa-color-neutral-800);
    }
    ::slotted(wa-dropdown-item)::part(base) {
      padding: 0.5rem 1rem;
    }
    :host([theme="dark"]) ::slotted(wa-dropdown-item) {
      color: white;
      --wa-color-neutral-600: white;
      --wa-color-neutral-500: white;
    }
    :host([theme="dark"]) ::slotted(wa-dropdown-item:hover) {
      background-color: rgba(255, 255, 255, 0.2);
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
