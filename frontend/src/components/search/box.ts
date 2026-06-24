import { LitElement, html, css } from 'lit';
import '@awesome.me/webawesome/dist/components/input/input.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';

export class SearchBox extends LitElement {
  static styles = css`
    :host { 
      display: inline-block; 
      width: 100%;
      max-width: 300px;
    }
    wa-input {
      --wa-input-background-color: rgba(255, 255, 255, 0.15);
      --wa-input-color: white;
      --wa-input-border-color: transparent;
      --wa-input-placeholder-color: rgba(255, 255, 255, 0.7);
    }
    wa-input::part(base) {
      border: none;
    }
    wa-input::part(input) {
      color: white;
    }
    wa-icon {
      color: rgba(255, 255, 255, 0.7);
      margin-left: 0.5rem;
    }
  `;

  render() {
    return html`
      <wa-input placeholder="Search..." pill>
        <wa-icon name="search" slot="prefix"></wa-icon>
      </wa-input>
    `;
  }
}

if (!customElements.get('search-box')) {
  customElements.define('search-box', SearchBox);
}
