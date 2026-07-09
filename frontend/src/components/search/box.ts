import { LitElement, html, css } from 'lit';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/input/input.js';

export class SearchBox extends LitElement {
  static styles = css`
    :host { 
      display: inline-block; 
      width: 100%;
      max-width: 300px;
    }
    .search-container {
      display: flex;
      align-items: flex-end;
      width: 100%;
    }
    wa-input {
      flex: 1;
      /* Backgrounds */
      --wa-form-control-background-color: transparent;
      --wa-color-neutral-fill-quiet: transparent;
      
      /* Text & Placeholders */
      --wa-form-control-value-color: white;
      --wa-form-control-placeholder-color: rgba(255, 255, 255, 0.6);
      
      /* Flat Underline Borders */
      --wa-form-control-border-color: rgba(255, 255, 255, 0.7);
      --wa-form-control-border-width: 0 0 1px 0;
      --wa-form-control-border-radius: 0;
      
      /* Disable Default Focus Ring */
      --wa-focus-ring-width: 0;
    }
    
    wa-input:focus-within {
      --wa-form-control-border-color: white;
      --wa-form-control-border-width: 0 0 2px 0;
    }

    wa-button {
      margin-left: 0.5rem;
      border-radius: 50%;
      box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
      --wa-color-neutral-fill-loud: rgba(255, 255, 255, 0.15);
      --wa-color-neutral-on-loud: white;
      --wa-color-neutral-border-loud: transparent;
    }
    wa-button:hover {
      --wa-color-neutral-fill-loud: rgba(255, 255, 255, 0.25);
    }
  `;

  render() {
    return html`
      <div class="search-container">
        <wa-input placeholder=""></wa-input>
        <wa-button variant="neutral" is-icon-button pill>
          <wa-icon name="search"></wa-icon>
        </wa-button>
      </div>
    `;
  }
}

if (!customElements.get('search-box')) {
  customElements.define('search-box', SearchBox);
}
