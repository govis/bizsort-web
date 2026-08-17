import { LitElement, html, css } from 'lit';
import type { ProductPreview } from '../types.js';

import '@awesome.me/webawesome/dist/components/icon/icon.js';

export class ProductCard extends LitElement {
  static get properties() {
    return {
      model: { type: Object },
      _imageLoaded: { state: true }
    };
  }

  declare model?: ProductPreview;
  declare private _imageLoaded: boolean;

  constructor() {
    super();
    this._imageLoaded = false;
  }

  private _getImageUrl(): string {
    if (!this.model?.image?.imageId) return '';
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
    return `${backendUrl}/api/image/get?entity=${this.model.image.entity}&id=${this.model.image.imageId}&width=240&height=180`;
  }

  private _handleClick() {
    if (this.model) {
      this.dispatchEvent(new CustomEvent('product-select', {
        composed: true,
        bubbles: true,
        detail: { id: this.model.id, name: this.model.name }
      }));
    }
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      width: 280px;
      max-width: 315px;
      border-radius: 12px;
      overflow: hidden;
      background: white;
      box-shadow: 0 2px 8px rgba(0,0,0,0.12);
      cursor: pointer;
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    :host(:hover) {
      transform: translateY(-4px);
      box-shadow: 0 8px 24px rgba(0,0,0,0.18);
    }

    .head {
      height: 180px;
      background-color: #cfd8dc;
      position: relative;
      overflow: hidden;
    }

    .head img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      opacity: 0;
      transition: opacity 0.3s ease;
    }

    .head img.loaded {
      opacity: 0.9;
    }

    .head .placeholder {
      position: absolute;
      inset: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 2.5rem;
      font-weight: bold;
      color: rgba(255,255,255,0.6);
      letter-spacing: 2px;
    }

    .body {
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      flex: 1;
    }

    .name {
      font-size: 16px;
      font-weight: 600;
      color: #1a237e;
      line-height: 1.3;
      cursor: pointer;
    }

    .name:hover {
      text-decoration: underline;
    }

    .info-row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 13px;
      color: #666;
    }

    .info-row wa-icon {
      font-size: 1rem;
      color: #999;
      flex-shrink: 0;
    }

    .text-preview {
      font-size: 12px;
      color: #888;
      line-height: 1.4;
      flex: 1;
      overflow: hidden;
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
    }
    
    .type-badge {
      display: inline-block;
      background: #e8eaf6;
      color: #3f51b5;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 500;
      align-self: flex-start;
      margin-top: 0.2rem;
    }
  `;

  render() {
    if (!this.model) return html``;

    const imgUrl = this._getImageUrl();
    const initials = this.model.name?.substring(0, 2).toUpperCase() || '??';

    return html`
      <div class="head" @click="${this._handleClick}">
        ${imgUrl ? html`
          <img src="${imgUrl}" alt="${this.model.name}"
               class="${this._imageLoaded ? 'loaded' : ''}"
               @load="${() => this._imageLoaded = true}" />
        ` : ''}
        ${!this._imageLoaded ? html`
          <div class="placeholder">${initials}</div>
        ` : ''}
      </div>
      <div class="body">
        <div class="name" @click="${this._handleClick}">${this.model.name}</div>
        
        ${this.model.type ? html`
          <div class="type-badge">${this.model.type.itemText}</div>
        ` : ''}
        
        ${this.model.category ? html`
          <div class="info-row">
            <wa-icon name="folder"></wa-icon>
            <span>${this.model.category.name}</span>
          </div>
        ` : ''}
        
        ${this.model.webUrl ? html`
          <div class="info-row">
            <wa-icon name="link"></wa-icon>
            <a href="${this.model.webUrl}" target="_blank" @click="${(e: Event) => e.stopPropagation()}" style="color: #666; text-decoration: none;">Website</a>
          </div>
        ` : ''}
        
        ${this.model.text ? html`
          <div class="text-preview">${this.model.text}</div>
        ` : ''}
      </div>
    `;
  }
}

if (!customElements.get('product-card')) {
  customElements.define('product-card', ProductCard);
}
