import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';
import type { CompanyPreview } from '../types.js';
import { getLogoUrl } from '../../service/image.js';
import { stringify } from '../../service/geocoder.js';

import '@awesome.me/webawesome/dist/components/checkbox/checkbox.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';

@customElement('company-listitem')
export class CompanyListItem extends LitElement {
  @property({ type: Object }) declare model?: CompanyPreview;
  @property({ type: Object }) declare itemOptions: any;
  @property({ type: Boolean, reflect: true }) declare selected: boolean;

  constructor() {
    super();
    this.selected = false;
    this.itemOptions = {};
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: row;
      width: 100%;
      background: white;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.12);
      margin-bottom: 0.5rem;
      overflow: hidden;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    :host(:hover) {
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    }

    .item-image {
      position: relative;
      width: 130px;
      flex-shrink: 0;
      background-color: #cfd8dc;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .item-image img {
      width: 100%;
      height: 100%;
      object-fit: contain;
    }

    .selection-bar {
      width: 4px;
      background-color: var(--wa-color-primary, #3b82f6);
      display: none;
    }

    :host([selected]) .selection-bar {
      display: block;
    }

    .item-body {
      display: flex;
      flex-direction: column;
      flex: 1;
      padding: 1rem;
      gap: 0.5rem;
    }

    .row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
    }

    .item-name {
      font-size: 1.1rem;
      font-weight: 600;
      color: #1a237e;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .item-name:hover {
      text-decoration: underline;
    }

    .item-action {
      display: flex;
      align-items: center;
      gap: 0.3rem;
      font-size: 0.85rem;
      color: #546e7a;
      text-decoration: none;
    }

    .item-action.link:hover {
      text-decoration: underline;
      color: var(--wa-color-primary, #3b82f6);
    }

    .item-text {
      font-size: 0.9rem;
      color: #78909c;
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
    
    wa-checkbox {
      position: absolute;
      top: 0.5rem;
      left: 0.5rem;
      background: rgba(255,255,255,0.9);
      border-radius: 4px;
    }
  `;

  private _handleCompanySelect() {
    if (this.model) {
      this.dispatchEvent(new CustomEvent('company-select', {
        composed: true, bubbles: true,
        detail: { id: this.model.id, name: this.model.name }
      }));
    }
  }

  private _handleProductsSelect(e: Event) {
    e.stopPropagation();
    e.preventDefault();
    if (this.model) {
      this.dispatchEvent(new CustomEvent('company-select', {
        composed: true, bubbles: true,
        detail: { id: this.model.id, name: this.model.name, tab: 'offerings' }
      }));
    }
  }

  private _selectedChanged(e: any) {
    this.selected = e.target.checked;
    this.dispatchEvent(new CustomEvent('item-select', {
        composed: true, bubbles: true,
        detail: { item: this.model, selected: this.selected }
    }));
  }

  render() {
    if (!this.model) return html``;

    const imgUrl = this.model.image?.imageId ? getLogoUrl(this.model.image.entity || 1, this.model.image.imageId, 130) : '';

    return html`
      ${this.itemOptions?.selectable ? html`<div class="selection-bar"></div>` : ''}
      
      <div class="item-image" @click="${this._handleCompanySelect}" style="cursor: pointer;">
        ${imgUrl ? html`<img src="${imgUrl}" alt="${this.model.name}" crossOrigin="anonymous">` : html`<wa-icon name="building" style="font-size: 2.5rem; color: #fff;"></wa-icon>`}
        
        ${this.itemOptions?.selectable ? html`
          <wa-checkbox ?checked="${this.selected}" @wa-change="${this._selectedChanged}" @click="${(e: Event) => e.stopPropagation()}"></wa-checkbox>
        ` : ''}
      </div>

      <div class="item-body">
        <div class="row">
          <div class="item-name" @click="${this._handleCompanySelect}">
            <wa-icon name="file-lines" style="color: var(--wa-color-primary, #3b82f6)"></wa-icon>
            ${this.model.name}
          </div>
          
          <div style="display: flex; gap: 1rem;">
            ${(this.model as any).productsView ? html`
              <a href="#" class="item-action link" @click="${this._handleProductsSelect}" title="View Products">
                <wa-icon name="box"></wa-icon> Offerings
              </a>
            ` : ''}
            
            ${this.model.webSite ? html`
              <a href="${this.model.webSite}" target="_blank" rel="noopener" class="item-action link" @click="${(e: Event) => e.stopPropagation()}">
                <wa-icon name="arrow-up-right-from-square"></wa-icon> ${this.model.webSite.replace(/^https?:\/\//, '')}
              </a>
            ` : ''}
          </div>
        </div>

        <div class="row">
          <div class="item-action">
            <wa-icon name="location-dot"></wa-icon>
            ${this.model.location?.address ? stringify(this.model.location.address, { postalCode: false, address1: false }) : 'Location unknown'}
          </div>
          
          ${this.model.category && this.itemOptions?.category !== false ? html`
            <div class="item-action link" style="cursor: pointer;" @click="${(e: Event) => {
                e.stopPropagation();
                this.dispatchEvent(new CustomEvent('app-navigate', { bubbles: true, composed: true, detail: { path: '/companies?categoryId=' + this.model?.category?.id } }));
            }}">
              <wa-icon name="folder"></wa-icon>
              ${this.model.category.name}
            </div>
          ` : ''}
        </div>

        <p class="item-text">${this.model.text}</p>
        <slot></slot>
      </div>
    `;
  }
}