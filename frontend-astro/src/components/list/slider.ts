import { LitElement, html, css, type TemplateResult, type CSSResultGroup } from 'lit';
import { property, state } from 'lit/decorators.js';

import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';

export abstract class ListSlider<T = any> extends LitElement {
  @state() declare protected _items: T[];
  @state() declare protected _loading: boolean;
  @state() declare protected _nextIndex: number;
  @property({ type: Number, attribute: 'company-id' }) declare companyId?: number;

  constructor() {
    super();
    this._items = [];
    this._loading = false;
    this._nextIndex = 0;
  }

  protected reset() {
    this._items = [];
    this._nextIndex = 0;
  }

  // Hook for derived classes to implement pagination fetching
  protected abstract fetchPage(): Promise<void>;
  
  // Hook for derived classes to render their specific card types
  protected abstract renderItem(item: T): TemplateResult;

  static styles: CSSResultGroup = css`
    :host {
      display: block;
      padding: 1rem 0;
    }

    .carousel {
      display: flex;
      gap: 1rem;
      overflow-x: auto;
      padding: 0.5rem;
      scroll-snap-type: x mandatory;
      scrollbar-width: thin;
    }

    .carousel::-webkit-scrollbar {
      height: 8px;
    }

    .carousel::-webkit-scrollbar-thumb {
      background: #ccc;
      border-radius: 4px;
    }

    .loading {
      display: flex;
      justify-content: center;
      padding: 2rem;
    }

    .load-more {
      display: flex;
      justify-content: center;
      padding: 1rem 0;
    }
  `;

  render() {
    return html`
      ${this._items.length > 0 ? html`
        <div class="carousel">
          ${this._items.map(item => this.renderItem(item))}
        </div>
      ` : ''}

      ${this._loading ? html`
        <div class="loading">
          <wa-spinner style="font-size: 2rem;"></wa-spinner>
        </div>
      ` : ''}

      ${!this._loading && this._nextIndex > 0 ? html`
        <div class="load-more">
          <wa-button variant="text" @click="${this.fetchPage}">
            <wa-icon slot="prefix" name="arrow-clockwise"></wa-icon>
            Show More
          </wa-button>
        </div>
      ` : ''}
    `;
  }
}
