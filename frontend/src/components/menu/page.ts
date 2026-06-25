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
      background-color: transparent;
      border: none;
    }
    :host([theme="dark"]) wa-button::part(base) {
      color: white;
      background-color: var(--primary-theme-color, #448aff);
      border: none;
    }
    :host([theme="dark"]) wa-icon {
      color: white;
    }
    :host([theme="dark"]) wa-dropdown {
      --wa-color-surface-raised: var(--primary-theme-color, #448aff);
      --wa-color-surface-border: transparent;
      --wa-color-text-normal: white;
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
      theme: { type: String, reflect: true }
    };
  }

  declare theme?: string;

  render() {
    return html`
      <wa-dropdown placement="bottom-end">
        <wa-button slot="trigger" variant="text" is-icon-button>
          <wa-icon name="ellipsis-vertical" library="system"></wa-icon>
        </wa-button>
        <slot></slot>
      </wa-dropdown>
    `;
  }
}

if (!customElements.get('page-menu')) {
  customElements.define('page-menu', PageMenu);
}
