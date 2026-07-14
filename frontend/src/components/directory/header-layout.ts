import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { Navigation, Company, Product } from '../../navigation';

import '../search/header';
import '@awesome.me/webawesome/dist/components/tab-group/tab-group.js';
import '@awesome.me/webawesome/dist/components/tab/tab.js';
import '@awesome.me/webawesome/dist/components/tab-panel/tab-panel.js';

@customElement('directory-header-layout')
export class DirectoryHeaderLayout extends LitElement {
  @property({ type: String, attribute: 'entity-type' })
  declare entityType: string;

  @property({ type: Number, attribute: 'category-id' })
  declare categoryId?: number;

  @property({ type: Number, attribute: 'location-id' })
  declare locationId?: number;

  @property({ type: String, attribute: 'search-query' })
  declare searchQuery?: string;

  @property({ type: String, attribute: 'search-near' })
  declare searchNear?: string;

  @state()
  declare private _condensed: boolean;

  constructor() {
    super();
    this.entityType = 'company';
    this._condensed = false;
  }

  connectedCallback() {
    super.connectedCallback();
    window.addEventListener('scroll', this._handleScroll);
  }

  disconnectedCallback() {
    window.removeEventListener('scroll', this._handleScroll);
    super.disconnectedCallback();
  }

  private _handleScroll = () => {
    const shouldCondense = window.scrollY > 72;
    if (this._condensed !== shouldCondense) {
      this._condensed = shouldCondense;
    }
  }

  private _onTabSelect(e: CustomEvent<{ name: string }>) {
    const targetTab = e.detail.name;
    if (targetTab === this.entityType) return;
    
    // In legacy, tab changes trigger navigation using the current tokens.
    // We'll mimic this by preserving params when navigating.
    const params: any = {};
    if (this.categoryId) params.categoryId = this.categoryId;
    if (this.locationId) params.locationId = this.locationId;
    if (this.searchQuery) params.searchQuery = this.searchQuery;
    if (this.searchNear) params.searchNear = this.searchNear;

    switch (targetTab) {
      case 'company':
        Navigation.go(Company.searchPage, params);
        break;
      case 'product':
        Navigation.go(Product.searchPage, params);
        break;
      // case 'project':
      //   Navigation.go('/project/search', params);
      //   break;
      // case 'job':
      //   Navigation.go('/job/search', params);
      //   break;
    }
  }

  static styles = css`
    :host { display: block; }
    
    .header-panel {
      background-color: var(--primary-theme-color, #448aff);
      position: sticky;
      top: -72px; /* search-header is 72px tall, so it scrolls away and navbar sticks at 0 */
      z-index: 100;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      transition: padding 0.3s;
    }
    
    search-header {
      background: var(--header-color2, #3367d6);
    }
    
    .navbar {
      max-width: 1200px;
      margin: 0 auto;
      display: flex;
      flex-direction: row;
      align-items: flex-end; /* Align tabs to bottom */
    }
    
    wa-tab-group {
      --indicator-color: white;
      --track-color: transparent;
      --track-width: 0.15rem;
      margin-left: 1rem;
    }
    
    wa-tab {
      color: rgba(255, 255, 255, 0.7);
    }
    wa-tab[active] {
      color: white;
      font-weight: bold;
    }
    
    .flex {
      flex: 1;
    }

    .main-content {
      max-width: 1200px;
      margin: 0 auto;
      background: #f5f5f5;
    }
  `;

  render() {
    if (this._condensed) {
      this.setAttribute('condensed', '');
    } else {
      this.removeAttribute('condensed');
    }

    return html`
      <div class="header-panel">
        <search-header 
          entity-type="${this.entityType}"
          .categoryId=${this.categoryId}
          .locationId=${this.locationId}
          .searchQuery=${this.searchQuery}
          .searchNear=${this.searchNear}
        ></search-header>
        
        <div class="navbar">
          <wa-tab-group @wa-tab-show="${this._onTabSelect}">
            <wa-tab slot="nav" panel="company" ?active="${this.entityType === 'company'}">Companies</wa-tab>
            <wa-tab slot="nav" panel="product" ?active="${this.entityType === 'product'}">Offerings</wa-tab>
            <wa-tab slot="nav" panel="project" ?active="${this.entityType === 'project'}">Projects</wa-tab>
            <wa-tab slot="nav" panel="job" ?active="${this.entityType === 'job'}">Jobs</wa-tab>
          </wa-tab-group>
          
          <div class="flex"></div>
          
          <div class="header-actions">
            <slot name="navbar"></slot>
            <slot name="dropdown"></slot>
          </div>
        </div>
      </div>

      <div class="main-content">
        <slot></slot>
      </div>
    `;
  }
}
