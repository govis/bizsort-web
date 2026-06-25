import { LitElement, html, css } from 'lit';

export class CompanyHeaderLayout extends LitElement {
  static get properties() {
    return {
      titleText: { type: String, attribute: 'title-text' },
      logoTextMain: { type: String, attribute: 'logo-text-main' },
      logoTextAccent: { type: String, attribute: 'logo-text-accent' },
      _condensed: { state: true }
    };
  }

  declare titleText?: string;
  declare logoTextMain?: string;
  declare logoTextAccent?: string;
  declare private _condensed: boolean;

  constructor() {
    super();
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
      min-height: 200px;
      position: sticky;
      top: 0;
      z-index: 100;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      transition: height 0.3s ease;
    }
    
    .header-actions {
      position: absolute;
      right: 1rem;
      top: 1rem;
      display: flex;
      gap: 0.5rem;
      align-items: center;
      z-index: 101;
    }
    
    .header-content-wrapper {
      max-width: 1000px;
      margin: 0 auto;
      display: flex;
      align-items: flex-end;
      padding: 0 1rem;
      height: 140px;
      position: relative;
      transition: height 0.3s ease;
    }
    
    /* Condensing logic */
    :host([condensed]) .header-content-wrapper {
      height: 60px; /* Condensed height */
    }
    
    .logo-container {
      width: 140px;
      height: 140px;
      background-color: white;
      border-radius: 4px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.2);
      border: 4px solid var(--primary-theme-color, #448aff);
      margin-bottom: -40px;
      margin-right: 2rem;
      z-index: 10;
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-weight: bold;
      font-size: 2rem;
      overflow: hidden;
      transition: opacity 0.2s ease, transform 0.2s ease;
      transform-origin: top center;
    }
    
    :host([condensed]) .logo-container {
      opacity: 0;
      pointer-events: none;
      transform: scale(0.8) translateY(-20px);
    }
    
    .logo-container ::slotted(img) {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
    
    .header-text-area {
      flex: 1;
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
      height: 100%;
      transition: transform 0.3s ease;
    }
    
    .company-title {
      font-size: 28px;
      font-weight: 500;
      margin: 0 0 1rem 0;
      color: white;
      transition: font-size 0.3s ease, margin 0.3s ease;
    }
    
    :host([condensed]) .company-title {
      font-size: 20px;
      margin: 0 0 0.25rem 0;
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
        <div class="header-actions">
          <slot name="navbar"></slot>
          <slot name="dropdown"></slot>
        </div>

        <div class="header-content-wrapper">
          <div class="logo-container">
             <slot name="logo">
                ${this.logoTextMain ? html`<span>${this.logoTextMain}</span>` : ''}
                ${this.logoTextAccent ? html`<span style="color:#ffeb3b;">${this.logoTextAccent}</span>` : ''}
             </slot>
          </div>
          
          <div class="header-text-area">
            <h1 class="company-title">${this.titleText || ''}</h1>
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
