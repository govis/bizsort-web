import { LitElement, html, css } from 'lit';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

export class PageMenu extends LitElement {
  static styles = css`
    :host { display: inline-block; }
    wa-icon {
      font-size: 24px;
    }
    :host([theme="dark"]) wa-button {
      --wa-color-neutral-fill-loud: var(--primary-theme-color, #448aff);
      --wa-color-neutral-on-loud: white;
      --wa-color-neutral-border-loud: var(--primary-theme-color, #448aff);
    }
    :host([theme="dark"]) wa-button:hover {
      --wa-color-neutral-fill-loud: rgba(255, 255, 255, 0.25);
    }
    :host([theme="dark"]) wa-icon {
      color: white;
      font-size: 24px;
    }
    :host([theme="dark"]) wa-dropdown {
      --wa-color-surface-raised: var(--primary-theme-color, #448aff);
      --wa-color-surface-border: transparent;
      --wa-panel-border-width: 0;
      color: white;
    }
    :host([theme="dark"]) ::slotted(wa-dropdown-item) {
      --wa-color-neutral-1000: white;
      --wa-color-neutral-900: white;
      --wa-color-neutral-800: white;
      --wa-color-neutral-700: white;
      --wa-color-neutral-600: white;
      --wa-color-neutral-500: white;
      color: white;
    }
    ::slotted(wa-dropdown-item) {
      color: var(--wa-color-neutral-800);
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
        <wa-button slot="trigger" variant="neutral" is-icon-button pill>
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
