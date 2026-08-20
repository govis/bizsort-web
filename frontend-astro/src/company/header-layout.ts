import { LitElement, html, css } from 'lit';
import { consume } from '@lit/context';
import { companyContext } from './context.js';
import { getLogoUrl, analyzeImage } from '../service/image.js';
import { Company } from '../navigation.js';
import { OfferingsView } from '../model/company.js';

export class CompanyHeaderLayout extends LitElement {
  static get properties() {
    return {
      company: { type: Object },
      activeTab: { type: String, attribute: 'active-tab' },
      _condensed: { state: true },
      noImage: { type: Boolean, attribute: 'no-image', reflect: true }
    };
  }

  @consume({ context: companyContext, subscribe: true })
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
            this.style.setProperty('--primary-theme-color', `rgb(${color.background.r},${color.background.g},${color.background.b})`);
            if (color.foreground) {
              this.style.setProperty('--header-name-color', `rgb(${color.foreground.r},${color.foreground.g},${color.foreground.b})`);
            }
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

    .name-placeholder {
      height: 84px;
    }
    
    .navbar {
      max-width: 1000px;
      margin: 0 auto;
      display: flex;
      flex-direction: row;
      align-items: center; /* Center items like search-box and dropdown by default */
      padding: 0 1rem;
    }
    
    .image-container {
      min-width: 100px;
      min-height: 26px;
      background-color: var(--primary-theme-color, #448aff);
      border-bottom-left-radius: 3px;
      border-bottom-right-radius: 3px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.2);
      margin: 0 15px -30px 0;
      padding: 0 6px 5px;
      box-sizing: border-box;
      z-index: 10;
      transform-origin: top center;
      align-self: flex-end; /* Force image to the bottom */
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
      align-self: flex-end; /* Force tabs to the bottom */
    }
    
    .name {
      height: 84px;
      display: flex;
      align-items: center;
      font-size: 28px;
      font-weight: 500;
      color: var(--header-name-color, white);
      padding-left: 16px;
      margin-top: -84px;
      margin-left: var(--name-margin-left, 0);
    }
    
    .tabs-and-actions {
      display: flex;
      flex-direction: row;
      /* align-items: stretch is default, so .actions will stretch to match the tabs height */
    }
    
    .tabs-wrapper {
      flex: 1;
      display: flex;
      align-items: flex-end; /* Keep tabs anchored to bottom */
    }
    
    .actions {
      display: flex;
      align-items: center; /* Center the buttons within the dynamically stretched height */
      gap: 4px;
    }
    
    .main-content {
      max-width: 1000px;
      margin: 60px auto 2rem auto;
      padding: 0 1rem;
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
        
        <div class="name-placeholder"></div>
        <div class="navbar">
          <div class="image-container shadow-2dp">
             <slot name="logo">
                ${logoUrl ? html`<img src="${logoUrl}" alt="${this.company?.name || ''} logo" crossOrigin="anonymous" @load="${this._imageLoaded}" />` : ''}
             </slot>
          </div>
          
          <div class="name-tabs">
            <div class="name">${this.company?.name || ''}</div>
            <div class="tabs-and-actions">
                <div class="tabs horizontal layout center">
                  ${this._renderTabs()}
                </div>
                <div class="actions">
                  <slot name="navbar"></slot>
                  <slot name="dropdown"></slot>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="main-content">
          <slot></slot>
        </div>
      `;
  }

  private _renderTabs() {
    if (!this.company) return html`<slot name="tabs"></slot>`; // Fallback if no company

    return html`
      <wa-button variant="${this.activeTab === 'about' ? 'primary' : 'text'}" 
                 size="medium"
                 @click="${() => this._handleTabClick('about')}">
        About
      </wa-button>
      
      ${this.company.offerings?.view ? html`
        <wa-button variant="${this.activeTab === 'offerings' ? 'primary' : 'text'}" 
                   size="medium"
                   @click="${() => this._handleTabClick('offerings')}">
          ${this.company.offerings.label || 'Offerings'}
        </wa-button>
      ` : ''}
      
      ${this.company.projects ? html`
        <wa-button variant="${this.activeTab === 'projects' ? 'primary' : 'text'}" 
                   size="medium"
                   @click="${() => this._handleTabClick('projects')}">
          ${this.company.projects.label || 'Projects'}
        </wa-button>
      ` : ''}
      
      ${this.company.jobs ? html`
        <wa-button variant="${this.activeTab === 'jobs' ? 'primary' : 'text'}" 
                   size="medium"
                   @click="${() => this._handleTabClick('jobs')}">
          ${this.company.jobs.label || 'Jobs'}
        </wa-button>
      ` : ''}
      
      ${this.company.news ? html`
        <wa-button variant="${this.activeTab === 'news' ? 'primary' : 'text'}" 
                   size="medium"
                   @click="${() => this._handleTabClick('news')}">
          ${this.company.news.label || 'News'}
        </wa-button>
      ` : ''}
      
      ${this.company.articles ? html`
        <wa-button variant="${this.activeTab === 'articles' ? 'primary' : 'text'}" 
                   size="medium"
                   @click="${() => this._handleTabClick('articles')}">
          ${this.company.articles.label || 'Articles'}
        </wa-button>
      ` : ''}
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
