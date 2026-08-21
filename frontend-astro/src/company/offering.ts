import { LitElement, html, css } from 'lit';

import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/spinner/spinner.js';

// Sub-components
import '../components/image/view';
import '../components/richtext/view';
import './header-layout';
import '../components/layout/card';

/**
 * Company Offering View.
 * Ported from legacy company/offering.ts.
 */
export class CompanyOffering extends LitElement {
  static get properties() {
    return {
      company: { type: Object },
      offering: { type: Object }
    };
  }

  declare company?: any;
  declare offering?: any;

  // Image logic moved to image-view component

  static styles = css`
    :host {
      display: block;
      font-family: Roboto, var(--wa-font-sans, sans-serif);
      background-color: #f5f5f5;
      min-height: 100vh;
      color: #333;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 4rem;
    }

    .content-center {
      width: 100%;
      max-width: 800px;
      margin-left: auto;
      margin-right: auto;
    }

    .content {
      margin-top: 25px;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    #mainCard {
      background-color: white;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.12);
      overflow: hidden;
    }

    .content-responsive {
      display: flex;
      flex-direction: column;
    }

    @media (min-width: 768px) {
      .content-responsive {
        flex-direction: row;
      }
      .image-section,
      .name-section {
        width: 50%;
      }
    }

    .image-section {
      background-color: #e0e0e0;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 300px;
    }

    .image-section img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .name-section {
      background-color: #37474f; /* paper-blue-grey-800 */
      color: #fff;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .offering-name {
      font-weight: 500;
      margin: 0;
      font-size: 24px;
    }

    .link-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .link-item {
      display: flex;
      align-items: center;
      gap: 12px;
      color: white;
      text-decoration: none;
      font-size: 16px;
    }

    .link-item wa-icon {
      font-size: 20px;
      color: rgba(255, 255, 255, 0.7);
    }

    .link-item:hover {
      text-decoration: underline;
    }

    .rich-text {
      padding: 16px;
      line-height: 1.5;
    }

    @keyframes slide-from-bottom {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    #mainCard {
      animation: slide-from-bottom 500ms cubic-bezier(0.4, 0, 0.2, 1) 200ms both;
    }

    #aboutCard {
      animation: slide-from-bottom 500ms cubic-bezier(0.4, 0, 0.2, 1) 400ms both;
    }
  `;

  render() {
    if (!this.company || !this.offering) {
      return html`
        <company-header-layout tab="offering">
          <div style="max-width:1000px; margin: 2rem auto;">Data not found.</div>
        </company-header-layout>
      `;
    }

    const images = this.offering?.images || [];

    return html`
      <company-header-layout active-tab="offerings" .company="${this.company}">
        <div id="contentWidth" class="content content-center">
          
          <div id="mainCard">
            <div class="content-responsive">
              <div class="image-section">
                ${images.length > 0 ? html`<image-view .images="${images}" alt="${this.offering.title}"></image-view>` : html`
                  <wa-icon name="image" style="font-size: 4rem; color: #999;"></wa-icon>
                `}
              </div>
              <div class="name-section">
                <h1 class="offering-name">${this.offering.title}</h1>
                
                <div class="link-list">
                  ${this.offering.webUrl ? html`
                    <a class="link-item" href="${this.offering.webUrl}" target="_blank" rel="noopener">
                      <wa-icon name="box-arrow-up-right"></wa-icon>
                      <span>Web page</span>
                    </a>
                  ` : ''}
                  
                  ${this.offering.category ? html`
                    <a class="link-item" href="/search?categoryId=${this.offering.category.id}">
                      <wa-icon name="folder"></wa-icon>
                      <span>${this.offering.category.name}</span>
                    </a>
                  ` : ''}
                </div>
              </div>
            </div>
          </div>

          <layout-card id="aboutCard" heading="${this.offering.type?.itemText || 'Offering'} Description">
            <div class="rich-text">
              ${this.offering.richText ? html`<richtext-view .html="${this.offering.richText}"></richtext-view>` : this.offering.text || 'No description available.'}
            </div>
          </layout-card>

        </div>
      </company-header-layout>
    `;
  }
}

if (!customElements.get('company-offering')) {
  customElements.define('company-offering', CompanyOffering);
}
