import { LitElement, html, css } from 'lit';

export class CompanyHeaderLayout extends LitElement {
  static get properties() {
    return {
      titleText: { type: String, attribute: 'title-text' },
      logoTextMain: { type: String, attribute: 'logo-text-main' },
      logoTextAccent: { type: String, attribute: 'logo-text-accent' },
      _condensed: { state: true },
      noImage: { type: Boolean, attribute: 'no-image', reflect: true }
    };
  }

  declare titleText?: string;
  declare logoTextMain?: string;
  declare logoTextAccent?: string;
  declare noImage: boolean;
  declare private _condensed: boolean;

  constructor() {
    super();
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
      align-items: flex-end; /* Align to the bottom so tabs are flush */
      padding: 0 1rem;
    }
    
    .image-container {
      width: 140px;
      height: 140px;
      background-color: var(--primary-theme-color, #448aff);
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
      color: white;
      font-weight: bold;
      font-size: 2.5rem;
      overflow: hidden;
      transition: opacity 0.2s ease, transform 0.2s ease;
      transform-origin: top center;
    }
    
    :host([condensed]) .image-container,
    :host([no-image]) .image-container {
      display: none;
    }
    
    .image-container ::slotted(img) {
      width: 100%;
      height: 100%;
      object-fit: contain;
      background-color: white;
      display: block;
    }
    
    .name-tabs {
      flex: 1;
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
      align-self: flex-end;
    }
    
    .name {
      height: 84px;
      margin-top: -84px;
      display: flex;
      align-items: center;
      font-size: 28px;
      font-weight: 500;
      color: white;
      padding-left: 16px;
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

    return html`
      <div class="header-panel">
        <div class="header-top">
          <slot name="navbar"></slot>
          <slot name="dropdown"></slot>
        </div>
        
        <div class="name-placeholder"></div>
        <div class="navbar">
          <div class="image-container shadow-2dp">
             <slot name="logo">
                ${this.logoTextMain ? html`<span>${this.logoTextMain}</span>` : ''}
                ${this.logoTextAccent ? html`<span style="color:#ffeb3b;">${this.logoTextAccent}</span>` : ''}
             </slot>
          </div>
          
          <div class="name-tabs">
            <div class="name">${this.titleText || ''}</div>
            <slot name="tabs"></slot>
          </div>
        </div>
      </div>

      <div class="main-content">
        <slot></slot>
      </div>
    `;
  }
}

if (!customElements.get('company-header-layout')) {
  customElements.define('company-header-layout', CompanyHeaderLayout);
}
