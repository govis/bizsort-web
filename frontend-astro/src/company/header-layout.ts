import { LitElement, html, css } from 'lit';
import { provide } from '@lit/context';
import { companyContext } from './context.js';
import { getLogoUrl, analyzeImage } from '../service/image.js';
import { Company } from '../navigation.js';
import { OfferingsView } from '../model/company.js';
import '@awesome.me/webawesome/dist/components/tab-group/tab-group.js';
import '@awesome.me/webawesome/dist/components/tab/tab.js';
import '@awesome.me/webawesome/dist/components/tab-panel/tab-panel.js';

export class CompanyHeaderLayout extends LitElement {
  static get properties() {
    return {
      company: { type: Object },
      activeTab: { type: String, attribute: 'active-tab' },
      _condensed: { state: true },
      noImage: { type: Boolean, attribute: 'no-image', reflect: true }
    };
  }

  @provide({ context: companyContext })
  declare company?: any;
  
  declare activeTab: string;
  declare noImage: boolean;
  declare private _condensed: boolean;

  constructor() {
    super();
    this.activeTab = 'about';
    this._condensed = false;
    this.noImage = false;
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
    // Condense the header if scrolled past 72px (matching legacy threshold)
    const shouldCondense = window.scrollY > 72;
    if (this._condensed !== shouldCondense) {
      this._condensed = shouldCondense;
    }
  }

  willUpdate(changed: Map<string, unknown>) {
    if (changed.has('company')) {
      const logoUrl = this.company ? getLogoUrl(this.company.image?.entity || 1, this.company.image?.imageId, 290, 145) : null;
      this.noImage = !logoUrl;
    }
  }

  private _imageLoaded(e: Event) {
    const img = e.target as HTMLImageElement;
    if (img) {
      if (img.clientHeight && img.clientHeight < 84) {
        this.style.setProperty('--name-margin-left', `-${img.clientWidth}px`);
        img.style.marginTop = `${84 - img.clientHeight}px`;
      } else {
        this.style.removeProperty('--name-margin-left');
        try {
          const color = analyzeImage(img, 5);
          if (color && color.background) {
            this.style.setProperty('--logo-bg-color', `rgb(${color.background.r},${color.background.g},${color.background.b})`);
          }
        } catch(err) {
          console.warn('Failed to analyze logo color', err);
        }
      }
    }
  }

  static styles = css`
    :host { display: block; }
    
    .header-panel {
      background-color: var(--primary-theme-color, #448aff);
      position: sticky;
      top: -72px; /* Scroll away the top 72px then stick */
      z-index: 100;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    
    .header-top {
      height: 72px;
      display: flex;
      align-items: center;
      justify-content: flex-end;
      padding: 0 1rem;
      max-width: 1000px;
      margin: 0 auto;
    }

    .navbar {
      max-width: 1000px;
      margin: 0 auto;
      display: flex;
      flex-direction: row;
      align-items: flex-end; /* Align to the bottom so tabs are flush */
      padding: 0 1rem;
    }
    
    .image-container {
      min-width: 100px;
      min-height: 26px;
      background-color: var(--logo-bg-color, var(--primary-theme-color, #448aff));
      border-bottom-left-radius: 3px;
      border-bottom-right-radius: 3px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.2);
      margin: 0 15px -30px 0;
      padding: 0 6px 5px;
      box-sizing: border-box;
      z-index: 10;
      flex-shrink: 0;
      display: flex;
      align-self: flex-end;
      align-items: center;
      justify-content: center;
      overflow: hidden;
      transition: opacity 0.2s ease, transform 0.2s ease;
      transform-origin: top center;
    }
    
    :host([condensed]) .image-container,
    :host([no-image]) .image-container {
      display: none;
    }
    
    .image-container ::slotted(img) {
      display: block; /* For proper padding like legacy */
      max-width: 200px; /* Optional sane cap */
    }
    
    .name-tabs {
      flex: 1;
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
    }
    
    .name {
      height: 84px;
      display: flex;
      align-items: center;
      font-size: 28px;
      font-weight: 500;
      color: var(--header-name-color, white);
      padding-left: 16px;
      margin-left: var(--name-margin-left, 0);
    }
    
    .header-actions {
      display: flex;
      flex-direction: row;
      align-items: center;
      margin-bottom: 8px; /* Align with tabs */
    }
    
    .main-content {
      max-width: 1000px;
      margin: 60px auto 2rem auto;
      padding: 0 1rem;
    }

    wa-tab-group {
      width: 100%;
    }
    wa-tab-group::part(body) {
      display: none;
    }
    wa-tab {
      --wa-color-neutral-on-quiet: rgba(255, 255, 255, 0.7);
      --wa-color-brand-on-quiet: white;
    }
    wa-tab-panel {
      display: none;
    }
  `;

  render() {
    // Reflect the condensed state to a host attribute for CSS targeting
    if (this._condensed) {
      this.setAttribute('condensed', '');
    } else {
      this.removeAttribute('condensed');
    }

    const logoUrl = this.company ? getLogoUrl(this.company.image?.entity || 1, this.company.image?.imageId, 290, 145) : null;

    return html`
      <div class="header-panel">
        <div class="header-top">
        </div>
        
        <div class="navbar">
          <div class="image-container shadow-2dp">
            ${logoUrl ? html`<img src="${logoUrl}" alt="${this.company?.name || ''} logo" crossOrigin="anonymous" @load="${this._imageLoaded}" />` : ''}
          </div>
          
          <div class="name-tabs">
            <div class="name">${this.company?.name || ''}</div>
            <div class="tabs horizontal layout center">
              ${this._renderTabs()}
            </div>
          </div>
          
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

  private _renderTabs() {
    if (!this.company) return html``;

    return html`
      <wa-tab-group style="--indicator-color: white; --track-color: transparent;" @wa-tab-show="${(e: any) => this._handleTabClick(e.detail.name)}">
        <wa-tab slot="nav" panel="about" ?active="${this.activeTab === 'about'}">About</wa-tab>
        
        ${this.company.offerings?.view ? html`
          <wa-tab slot="nav" panel="offerings" ?active="${this.activeTab === 'offerings'}">
            ${this.company.offerings.label || 'Offerings'}
          </wa-tab>
        ` : ''}
        
        ${/* this.company.projects ? html`
          <wa-tab slot="nav" panel="projects" ?active="${this.activeTab === 'projects'}">
            ${this.company.projects.label || 'Projects'}
          </wa-tab>
        ` : '' */ ''}
        
        ${/* this.company.jobs ? html`
          <wa-tab slot="nav" panel="jobs" ?active="${this.activeTab === 'jobs'}">
            ${this.company.jobs.label || 'Jobs'}
          </wa-tab>
        ` : '' */ ''}
        
        ${/* this.company.news ? html`
          <wa-tab slot="nav" panel="news" ?active="${this.activeTab === 'news'}">
            ${this.company.news.label || 'News'}
          </wa-tab>
        ` : '' */ ''}
        
        ${/* this.company.articles ? html`
          <wa-tab slot="nav" panel="articles" ?active="${this.activeTab === 'articles'}">
            ${this.company.articles.label || 'Articles'}
          </wa-tab>
        ` : '' */ ''}
        
        <!-- We must include empty panels for wa-tab-group to render properly -->
        <wa-tab-panel name="about"></wa-tab-panel>
        <wa-tab-panel name="offerings"></wa-tab-panel>
        <!--
        <wa-tab-panel name="projects"></wa-tab-panel>
        <wa-tab-panel name="jobs"></wa-tab-panel>
        <wa-tab-panel name="news"></wa-tab-panel>
        <wa-tab-panel name="articles"></wa-tab-panel>
        -->
      </wa-tab-group>
    `;
  }

  private _handleTabClick(tab: string) {
    if (tab === this.activeTab) return;

    if (tab === 'offerings') {
      // If MultiOffering view, don't navigate! Just emit tab-change for the host to render the rich-text.
      if (this.company.offerings?.view === OfferingsView.MultiOffering) {
        this.dispatchEvent(new CustomEvent('tab-change', { detail: { value: tab }, bubbles: true, composed: true }));
        return;
      }
    }

    // Default routing behavior for all other tabs (or OfferingList view)
    if (tab === 'about') {
      Company.profileView(this.company.id);
    } else {
      Company.tabView(this.company.id, tab);
    }
  }
}

if (!customElements.get('company-header-layout')) {
  customElements.define('company-header-layout', CompanyHeaderLayout);
}
