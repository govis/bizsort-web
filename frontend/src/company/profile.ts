import { LitElement, html, css } from 'lit';
import { repeat } from 'lit/directives/repeat.js';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';
import type { Company, Office } from '../components/types.js';

// Web Awesome components
import '@awesome.me/webawesome/dist/components/select/select.js';
import '@awesome.me/webawesome/dist/components/option/option.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '@awesome.me/webawesome/dist/components/tab-group/tab-group.js';
import '@awesome.me/webawesome/dist/components/tab/tab.js';
import '@awesome.me/webawesome/dist/components/tab-panel/tab-panel.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';

// Building block components
import '../components/search/box';
import '../components/product/slider';
import './header-layout';
import '../components/layout/card';
import '../components/menu/page';
import '../components/search/category/menu';
import '../components/map/view';

export class CompanyProfile extends LitElement {
  static get properties() {
    return {
      companyId: { type: Number, attribute: 'company-id' },
      _company: { state: true },
      _selectedOffice: { state: true },
      _loading: { state: true },
      _error: { state: true },
      activeTab: { type: String, attribute: 'active-tab' }
    };
  }

  declare companyId?: number;
  declare private _company?: Company;
  declare private _selectedOffice?: Office;
  declare private _loading: boolean;
  declare private _error?: string;
  declare activeTab: string;

  constructor() {
    super();
    this._loading = false;
    this.activeTab = 'about';
  }

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if (changedProperties.has('companyId') && this.companyId) {
      this._fetchCompany();
    }
  }

  private async _fetchCompany() {
    this._loading = true;
    this._error = undefined;
    try {
      const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
      const response = await fetch(`${backendUrl}/api/company/profile/view?company=${this.companyId}`);
      if (!response.ok) throw new Error('Failed to fetch company');
      this._company = await response.json();
      this._selectedOffice = this._company?.headOffice || this._company?.offices[0];
    } catch (e: unknown) {
      this._error = e instanceof Error ? e.message : 'An unknown error occurred';
    } finally {
      this._loading = false;
    }
  }

  private _handleOfficeChange(e: Event) {
    const officeId = (e.target as HTMLSelectElement).value;
    this._selectedOffice = this._company?.offices.find(o => o.id.toString() === officeId);
  }

  private _officeName(office: Office, index: number) {
    if (office.id === this._company?.headOffice?.id || index === 0) return "Head Office";
    return office.name || "Office";
  }

  private _getOsmMapUrl(office?: Office) {
    if (!office?.location?.geoLocation) return '';
    const { lat, lng } = office.location.geoLocation;
    const offset = 0.01;
    const bbox = `${lng - offset},${lat - offset},${lng + offset},${lat + offset}`;
    return `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${lat},${lng}`;
  }

  private _getLogoUrl(): string {
    if (!this._company?.image?.imageId) return '';
    const backendUrl = process.env.NEXT_PUBLIC_API_URL || '';
    return `${backendUrl}/api/image/get?entity=${this._company.image.entity}&id=${this._company.image.imageId}&w=140&h=140`;
  }

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

    /* Entry animations */
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

    .contact-section {
      animation: slide-from-bottom 500ms cubic-bezier(0.4, 0, 0.2, 1) 200ms;
      animation-fill-mode: both;
    }

    .about-section {
      animation: slide-from-bottom 500ms cubic-bezier(0.4, 0, 0.2, 1) 400ms;
      animation-fill-mode: both;
    }

    .tab-section {
      animation: slide-from-bottom 500ms cubic-bezier(0.4, 0, 0.2, 1) 200ms;
      animation-fill-mode: both;
    }

    /* Contact & Map layout */
    .contact-grid {
      display: flex;
      flex-direction: column;
    }

    @media (min-width: 768px) {
      .contact-grid {
        flex-direction: row;
      }
      .contact-info-pane {
        width: 40%;
        min-width: 350px;
      }
      .map-pane {
        flex: 1; /* takes remaining 60% */
      }
    }

    .contact-info-pane {
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .map-pane {
      background-color: #e5e3df;
      min-height: 350px;
      display: flex;
      flex-direction: column;
      position: relative;
    }

    .map-frame {
      width: 100%;
      height: 100%;
      border: none;
      flex-grow: 1;
    }

    .office-select {
      width: 100%;
      margin-bottom: 0.5rem;
    }

    /* Info Items */
    .info-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .info-item {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      padding: 0.5rem 0;
      text-decoration: none;
      color: #333;
      font-size: 15px;
      cursor: pointer;
    }

    .info-item:hover {
      background-color: rgba(0,0,0,0.02);
    }

    .info-item wa-icon {
      font-size: 1.25rem;
      color: #666;
      margin-top: 2px;
    }

    .category-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
    }

    .category-item-left {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .rich-text p {
      margin-top: 0;
    }

    .map-unavailable {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #888;
    }

    .map-unavailable wa-icon {
      font-size: 3rem;
      margin-bottom: 1rem;
    }

    .map-click-overlay {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      z-index: 10;
      cursor: pointer;
    }
  `;

  render() {
    if (this._loading) {
      return html`
        <div class="loading-container">
          <wa-spinner style="font-size: 3rem;"></wa-spinner>
        </div>
      `;
    }

    if (this._error) {
      return html`<div style="max-width:1000px; margin: 2rem auto; color: red;">Error: ${this._error}</div>`;
    }

    if (!this._company) return html`<div style="max-width:1000px; margin: 2rem auto;">Company not found.</div>`;

    const hasMultipleOffices = this._company.offices && this._company.offices.length > 1;
    const logoUrl = this._getLogoUrl();

    return html`
      <div class="company-profile-content">
        ${this.activeTab === 'about' ? this._renderAboutTab(hasMultipleOffices) : ''}
        ${this.activeTab === 'products' ? this._renderProductsTab() : ''}
        ${this.activeTab === 'projects' ? this._renderStubTab('projects', this._company.projects?.label || 'Projects') : ''}
        ${this.activeTab === 'jobs' ? this._renderStubTab('jobs', this._company.jobs?.label || 'Jobs') : ''}
        ${this.activeTab === 'marketplace' ? this._renderStubTab('marketplace', this._company.marketplace?.label || 'Marketplace') : ''}
        ${this.activeTab === 'promotions' ? this._renderStubTab('promotions', this._company.promotions?.label || 'Promotions') : ''}
        ${this.activeTab === 'news' ? this._renderStubTab('news', this._company.news?.label || 'News') : ''}
        ${this.activeTab === 'articles' ? this._renderArticlesTab() : ''}
      </div>
      <map-view id="mapView"></map-view>
    `;
  }

  private _renderAboutTab(hasMultipleOffices: boolean) {
    return html`
      <layout-card class="contact-section" no-padding>
        <div class="contact-grid">
          <div class="contact-info-pane">
            ${hasMultipleOffices ? html`
              <wa-select
                class="office-select"
                value="${this._selectedOffice?.id.toString()}"
                @wa-change="${this._handleOfficeChange}"
              >
                ${repeat(this._company!.offices, (o) => o.id, (o, index) => html`
                  <wa-option value="${o.id.toString()}">${this._officeName(o, index)}</wa-option>
                `)}
              </wa-select>
            ` : ''}

            <div class="info-list">
              ${this._selectedOffice ? html`
                <div class="info-item" @click="${() => window.open(`https://www.google.com/maps/search/?api=1&query=${this._selectedOffice?.location?.geoLocation?.lat},${this._selectedOffice?.location?.geoLocation?.lng}`)}">
                  <wa-icon name="geo-alt"></wa-icon>
                  <span>${this._selectedOffice.location?.address}</span>
                </div>

                <div class="info-item">
                  <wa-icon name="telephone"></wa-icon>
                  <span>${this._selectedOffice.phone}${this._selectedOffice.phone1 ? `, ${this._selectedOffice.phone1}` : ''}</span>
                </div>

                ${this._selectedOffice.fax ? html`
                  <div class="info-item">
                    <wa-icon name="printer"></wa-icon>
                    <span>${this._selectedOffice.fax}</span>
                  </div>
                ` : ''}
              ` : ''}

              ${this._company!.email ? html`
                <a href="mailto:${this._company!.email}" class="info-item">
                  <wa-icon name="envelope"></wa-icon>
                  <span>Email</span>
                </a>
              ` : ''}

              ${this._company!.webSite ? html`
                <a href="${this._company!.webSite}" target="_blank" rel="noopener" class="info-item">
                  <wa-icon name="box-arrow-up-right"></wa-icon>
                  <span>${this._company!.webSite}</span>
                </a>
              ` : ''}

              ${this._company!.category ? html`
                <div class="info-item">
                  <div class="category-item">
                    <div class="category-item-left">
                      <wa-icon name="folder"></wa-icon>
                      <span>${this._company!.category.name}</span>
                    </div>
                    <search-category-menu 
                      .category="${this._company!.category}"
                      .location="${this._selectedOffice?.location}">
                    </search-category-menu>
                  </div>
                </div>
              ` : ''}

              ${this._company!.appUri ? html`
                <a href="${this._company!.appUri}" target="_blank" rel="noopener" class="info-item">
                  <wa-icon name="phone"></wa-icon>
                  <span>Mobile App</span>
                </a>
              ` : ''}
            </div>
          </div>

          <div class="map-pane">
            ${this._selectedOffice?.location?.geoLocation ? html`
              <iframe
                class="map-frame"
                src="${this._getOsmMapUrl(this._selectedOffice)}"
                scrolling="no"
                marginheight="0"
                marginwidth="0">
              </iframe>
              <div class="map-click-overlay" @click="${() => {
                const map = this.shadowRoot?.getElementById('mapView') as any;
                if (map) map.open(this._company?.offices);
              }}" title="View Full Map"></div>
            ` : html`
              <div class="map-unavailable">
                <wa-icon name="map"></wa-icon>
                <span>Map not available</span>
              </div>
            `}
          </div>
        </div>
      </layout-card>

      ${this._company!.richText || this._company!.description ? html`
        <layout-card class="about-section" heading="About ${this._company!.name}">
          <div class="rich-text">
            ${this._company!.richText ? unsafeHTML(this._company!.richText) : this._company!.description}
          </div>
        </layout-card>
      ` : ''}
    `;
  }

  private _renderProductsTab() {
    const productRefs = (this._company!.offerings as any)?.items || []; // Assume items has product refs
    return html`
      <layout-card class="tab-section" heading="${this._company!.offerings?.label || 'What We Do'}">
        <div class="rich-text">
          ${this._company!.offerings?.multiProduct ? unsafeHTML(this._company!.offerings.multiProduct) : ''}
        </div>
        ${productRefs.length > 0 ? html`
          <product-slider .companyId="${this.companyId}" .productRefs="${productRefs}"></product-slider>
        ` : ''}
      </layout-card>
    `;
  }

  private _renderArticlesTab() {
    return html`
      <layout-card class="tab-section" heading="${this._company!.articles?.label || 'Articles'}">
        <div class="rich-text">
          <p style="color: #666; text-align: center;">No articles available.</p>
        </div>
      </layout-card>
    `;
  }

  private _renderStubTab(id: string, label: string) {
    return html`
      <layout-card class="tab-section" heading="${label}">
        <div class="rich-text">
          <p style="color: #666; text-align: center;">The ${id} section is not yet available.</p>
        </div>
      </layout-card>
    `;
  }
}

if (!customElements.get('company-profile')) {
  customElements.define('company-profile', CompanyProfile);
}
