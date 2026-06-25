import { LitElement, html, css } from 'lit';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';

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
    input {
      flex: 1;
      background: transparent;
      border: none;
      border-bottom: 1px solid rgba(255, 255, 255, 0.7);
      color: white;
      padding: 0.5rem 0;
      font-size: 1rem;
      outline: none;
      transition: border-bottom-color 0.2s;
    }
    input:focus {
      border-bottom-color: white;
    }
    input::placeholder {
      color: rgba(255, 255, 255, 0.6);
    }
    wa-button::part(base) {
      border-radius: 50%;
      width: 40px;
      height: 40px;
      padding: 0;
      background-color: rgba(255, 255, 255, 0.15);
      color: white;
      border: none;
      margin-left: 0.5rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    wa-icon {
      font-size: 24px;
    }
    wa-button::part(base):hover {
      background-color: rgba(255, 255, 255, 0.25);
    }
  `;

  render() {
    return html`
      <div class="search-container">
        <input type="text" placeholder="" />
        <wa-button>
          <wa-icon name="search"></wa-icon>
        </wa-button>
      </div>
    `;
  }
}

if (!customElements.get('search-box')) {
  customElements.define('search-box', SearchBox);
}
