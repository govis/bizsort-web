import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import type { CompanyPreview } from '../types.js';
import { getLogoUrl, analyzeImage } from '../../service/image.js';
import { stringify } from '../../service/geocoder';

import '@awesome.me/webawesome/dist/components/icon/icon.js';

export class CompanyCard extends LitElement {
@property({ type: Object })
  declare model?: CompanyPreview;
  @state()
  declare private _imageLoaded: boolean;

  constructor() {
    super();
    this._imageLoaded = false;
  }

  private _handleClick() {
    if (this.model) {
      this.dispatchEvent(new CustomEvent('company-select', {
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
      height: 120px;
      background-color: var(--card-header-background, #cfd8dc);
      position: relative;
      overflow: hidden;
    }

    .head img {
      width: 100%;
      height: 100%;
      object-fit: contain;
      opacity: 0;
      transition: opacity 0.3s ease;
    }

    .head img.loaded {
      opacity: 1;
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
      color: #546e7a;
    }

    wa-icon {
      font-size: 14px;
      color: #90a4ae;
    }

    .text-preview {
      font-size: 13px;
      color: #78909c;
      margin-top: 0.5rem;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    :host(:hover) .collapsible {
      display: none;
    }

    .unobtrusive {
      display: none;
    }

    :host(:hover) .unobtrusive {
      display: -webkit-box;
    }
  `;

  render() {
    if (!this.model) return html``;

    const imgUrl = this.model.image?.imageId ? getLogoUrl(this.model.image.entity || 1, this.model.image.imageId, 240, 120) : '';
    const initials = this.model.name?.substring(0, 2).toUpperCase() || '??';

    return html`
      <div class="head" @click="${this._handleClick}">
        ${imgUrl ? html`
          <img src="${imgUrl}" alt="${this.model.name}"
               crossOrigin="anonymous"
               class="${this._imageLoaded ? 'loaded' : ''}"
               @load="${this._onImageLoad}" />
        ` : ''}
        ${!this._imageLoaded ? html`
          <div class="placeholder">${initials}</div>
        ` : ''}
      </div>
      <div class="body">
        <div class="name" @click="${this._handleClick}">${this.model.name}</div>
        ${this.model.category ? html`
          <div class="info-row">
            <wa-icon name="folder"></wa-icon>
            <span>${this.model.category.name}</span>
          </div>
        ` : ''}
        ${this.model.location?.address ? html`
          <div class="info-row collapsible">
            <wa-icon name="location-dot"></wa-icon>
            <span>${stringify(this.model.location.address, { postalCode: false, address1: false })}</span>
          </div>
        ` : ''}
        ${this.model.text ? html`
          <div class="text-preview unobtrusive">${this.model.text}</div>
        ` : ''}
      </div>
    `;
  }
}

if (!customElements.get('company-card')) {
  customElements.define('company-card', CompanyCard);
}
