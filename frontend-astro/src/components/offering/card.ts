import { LitElement, html, css } from 'lit';
import type { OfferingPreview } from '../types.js';
import { getLogoUrl, analyzeImage, ImageSizes } from '../../service/image.js';

import '@awesome.me/webawesome/dist/components/icon/icon.js';

export class OfferingCard extends LitElement {
  static get properties() {
    return {
      model: { type: Object },
      _imageLoaded: { state: true }
    };
  }

  declare model?: OfferingPreview;
  declare private _imageLoaded: boolean;

  constructor() {
    super();
    this._imageLoaded = false;
  }

  private _getImageUrl(): string {
    if (!this.model?.image) return '/images/bizsort-logo.svg';
    return getLogoUrl(
      this.model.image.entity,
      this.model.image.imageId,
      ImageSizes.Offering.Card.width,
      ImageSizes.Offering.Card.height
    );
  }

  private _handleClick() {
    if (this.model) {
      this.dispatchEvent(new CustomEvent('offering-select', {
        composed: true,
        bubbles: true,
        detail: { id: this.model.id, name: this.model.name }
      }));
    }
  }

  private _onImageLoad(e: Event) {
    this._imageLoaded = true;
    const img = e.target as HTMLImageElement;
    if (img) {
      try {
        const color = analyzeImage(img, 5);
        if (color.background.r < 235 || color.background.g < 235 || color.background.b < 235) {
          this.style.setProperty('--card-header-background', `rgb(${color.background.r},${color.background.g},${color.background.b})`);
        }
      } catch (err) {
        console.warn('Failed to analyze image color', err);
      }
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
      background-color: var(--card-header-background, #cfd8dc);
      position: relative;
      overflow: hidden;
      transition: background-color 0.3s ease;
    }

    .head img {
      width: 100%;
      height: 100%;
      object-fit: contain;
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
    }

    .name:hover {
      text-decoration: underline;
    }

    .info-row {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 13px;
      color: #666;
    }

    .info-row wa-icon {
      font-size: 14px;
      color: #888;
    }

    .type-badge {
      align-self: flex-start;
      background: #e8eaf6;
      color: #3f51b5;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
    }

    .text-preview {
      margin-top: 4px;
      font-size: 13px;
      color: #444;
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
      line-height: 1.4;
    }
  `;

  render() {
    if (!this.model) return html``;

    const initials = this.model.name ? this.model.name.substring(0, 2).toUpperCase() : 'O';
    const imgUrl = this._getImageUrl();

    return html`
      <div class="head" @click="${this._handleClick}">
        ${imgUrl ? html`
          <img src="${imgUrl}" alt="${this.model.name}"
               crossorigin="anonymous"
               class="${this._imageLoaded ? 'loaded' : ''}"
               @load="${this._onImageLoad}" />
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

if (!customElements.get('offering-card')) {
  customElements.define('offering-card', OfferingCard);
}
