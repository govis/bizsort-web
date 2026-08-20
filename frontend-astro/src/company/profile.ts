import { LitElement, html, css } from 'lit';
import { repeat } from 'lit/directives/repeat.js';
import { unsafeHTML } from 'lit/directives/unsafe-html.js';
import { companyContext } from './context.js';
import type { Company, Office } from '../components/types.js';

// Web Awesome components
import '@awesome.me/webawesome/dist/components/select/select.js';
import '@awesome.me/webawesome/dist/components/option/option.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/button/button.js';
import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';
import '@awesome.me/webawesome/dist/components/dropdown/dropdown.js';
import '@awesome.me/webawesome/dist/components/dropdown-item/dropdown-item.js';
import { getLogoUrl } from '../service/image.js';

// Building block components
import '../components/search/box';
import '../components/offering/slider';
import './header-layout';
import '../components/layout/card';
import '../components/menu/page';
import '../components/search/category/menu';
import '../components/map/view';
import '../components/offering/slider';
import '../components/company/slider';
import '../components/community/slider';
import { stringify } from '../service/geocoder';

export class CompanyProfile extends LitElement {
  static get properties() {
    return {
      companyId: { type: Number, attribute: 'company-id' },
      company: { type: Object },
      _selectedOffice: { state: true },
      activeTab: { type: String, attribute: 'active-tab' }
    };
  }

  declare companyId?: number;
  
  declare company?: Company;
  declare private _selectedOffice?: Office;
  declare activeTab: string;

  constructor() {
    super();
    this.activeTab = 'about';
  }

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if (changedProperties.has('company') && this.company) {
      this._selectedOffice = this.company.headOffice || this.company.offices?.[0];
    }
  }

  private _handleOfficeChange(e: Event) {
    const officeId = (e.target as HTMLSelectElement).value;
    this._selectedOffice = this.company?.offices.find(o => o.id.toString() === officeId);
  }

  private _officeName(office: Office, index: number) {
    if (office.id === this.company?.headOffice?.id || index === 0) return "Head Office";
    return office.name || "Office";
  }

  private _getOsmMapUrl(office?: Office) {
    if (!office?.location?.geoLocation) return '';
    const { lat, lng } = office.location.geoLocation;
    const offset = 0.01;
    const bbox = `${lng - offset},${lat - offset},${lng + offset},${lat + offset}`;
    return `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${lat},${lng}`;
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

    wa-select.office-select {
      width: calc(100% - 22px);
      margin-bottom: 0.5rem;
      margin-left: 22px; /* 36px total minus ~14px internal wa-select padding */
      
      /* Make it look like an underlined text field */
      border-bottom: 1px solid #78909c; /* Legacy dark-grey color to ensure it's visible */
      
      /* Hide all WebAwesome internal borders and focus rings */
      --wa-form-control-border-color: transparent;
      --wa-input-focus-ring-color: transparent;
      --wa-input-border-color-focus: transparent;
      
      /* Background transparent for both form-control and neutral palette */
      --wa-form-control-background-color: transparent;
      --wa-color-neutral-fill-quiet: transparent;
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
        align-items: flex-start;
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
    if (!this.company) return html`<div style="max-width:1000px; margin: 2rem auto;">Company not found.</div>`;

    const hasMultipleOffices = this.company.offices && this.company.offices.length > 1;

    // Map activeTab from URL query params (default to about)
    const url = new URL(window.location.href);
    const tabFromUrl = url.searchParams.get('tab');
    if (tabFromUrl && tabFromUrl !== this.activeTab) {
      this.activeTab = tabFromUrl;
    }

    return html`
      <company-header-layout .company="${this.company}" @tab-change="${this._handleTabChange}">
        <search-box slot="navbar"></search-box>

        <page-menu slot="dropdown" theme="dark">
          <wa-dropdown-item>
            <wa-icon slot="icon" name="pen"></wa-icon>
            Add your Company
          </wa-dropdown-item>
          <wa-dropdown-item>
            <wa-icon slot="icon" name="tag"></wa-icon>
            Tag this Company
          </wa-dropdown-item>
          <wa-dropdown-item>
            <wa-icon slot="icon" name="share-nodes"></wa-icon>
            Share with Community
          </wa-dropdown-item>
        </page-menu>

        <div class="company-profile-content">
          ${this.activeTab === 'about' ? this._renderAboutTab(hasMultipleOffices) : ''}
          ${this.activeTab === 'offerings' ? this._renderOfferingsTab() : ''}
          ${this.activeTab === 'projects' ? this._renderStubTab('projects', this.company.projects?.label || 'Projects') : ''}
          ${this.activeTab === 'jobs' ? this._renderStubTab('jobs', this.company.jobs?.label || 'Jobs') : ''}
          ${this.activeTab === 'marketplace' ? this._renderStubTab('marketplace', this.company.marketplace?.label || 'Marketplace') : ''}
          ${this.activeTab === 'promotions' ? this._renderStubTab('promotions', this.company.promotions?.label || 'Promotions') : ''}
          ${this.activeTab === 'news' ? this._renderStubTab('news', this.company.news?.label || 'News') : ''}
          ${this.activeTab === 'articles' ? this._renderArticlesTab() : ''}
        </div>
      </company-header-layout>
      <map-view id="mapView"></map-view>
    `;
  }

    private _handleTabChange(e: any) {
      const tabName = e.detail?.value || e.detail?.name;
      if (!tabName) return;
      
      this.activeTab = tabName;
    const url = new URL(window.location.href);
    if (tabName === 'about') {
      url.searchParams.delete('tab');
    } else {
      url.searchParams.set('tab', tabName);
    }
    window.history.replaceState(null, '', url.toString());
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
                @change="${this._handleOfficeChange}"
              >
                ${repeat(this.company!.offices, (o) => o.id, (o, index) => html`
                  <wa-option value="${o.id.toString()}">${this._officeName(o, index)}</wa-option>
                `)}
              </wa-select>
            ` : ''}

            <div class="info-list">
              ${this._selectedOffice ? html`
                <div class="info-item" @click="${() => window.open(`https://www.google.com/maps/search/?api=1&query=${this._selectedOffice?.location?.geoLocation?.lat},${this._selectedOffice?.location?.geoLocation?.lng}`)}">
                  <wa-icon name="location-dot"></wa-icon>
                  <span>
                    ${stringify(this._selectedOffice.location?.address)}
                  </span>
                </div>

                <div class="info-item">
                  <wa-icon name="phone"></wa-icon>
                  <span>${this._selectedOffice.phone}${this._selectedOffice.phone1 ? `, ${this._selectedOffice.phone1}` : ''}</span>
                </div>

                ${this._selectedOffice.fax ? html`
                  <div class="info-item">
                    <wa-icon name="print"></wa-icon>
                    <span>${this._selectedOffice.fax}</span>
                  </div>
                ` : ''}
              ` : ''}

              ${this.company!.email ? html`
                <a href="mailto:${this.company!.email}" class="info-item">
                  <wa-icon name="envelope"></wa-icon>
                  <span>Email</span>
                </a>
              ` : ''}

              ${this.company!.webSite ? html`
                <a href="${this.company!.webSite}" target="_blank" rel="noopener" class="info-item">
                  <wa-icon name="arrow-up-right-from-square"></wa-icon>
                  <span>${this.company!.webSite}</span>
                </a>
              ` : ''}

              ${this.company!.category ? html`
                <div class="info-item" style="padding-top: 0; padding-bottom: 0;">
                  <div class="category-item">
                    <div class="category-item-left">
                      <wa-icon name="folder"></wa-icon>
                      <span style="margin-top: 5px;">${this.company!.category.name}</span>
                    </div>
                    <search-category-menu 
                      .category="${this.company!.category}"
                      .location="${this._selectedOffice?.location}">
                    </search-category-menu>
                  </div>
                </div>
              ` : ''}

              ${this.company!.appUri ? html`
                <a href="${this.company!.appUri}" target="_blank" rel="noopener" class="info-item">
                  <wa-icon name="mobile-screen-button"></wa-icon>
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
                if (map) map.open(this.company?.offices);
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

      ${this.company!.richText || this.company!.description ? html`
        <layout-card class="about-section" heading="About ${this.company!.name}">
          <div class="rich-text">
            ${this.company!.richText ? unsafeHTML(this.company!.richText) : this.company!.description}
          </div>
        </layout-card>
      ` : ''}

      ${(this.company!.offerings as any)?.items?.length > 0 ? html`
        <div class="slider-container" style="margin-top: 2rem;">
          <h2 style="font-size: 1.25rem; margin-bottom: 1rem; color: #333; text-align: center;">Featured Offerings and Services</h2>
          <offering-slider .companyId="${this.companyId}" .offeringRefs="${(this.company!.offerings as any)?.items}"></offering-slider>
        </div>
      ` : ''}

      ${this.company!.hasAffiliations ? html`
        <div class="slider-container" style="margin-top: 2rem;">
          <h2 style="font-size: 1.25rem; margin-bottom: 1rem; color: #333; text-align: center;">Company Affiliations</h2>
          <company-slider .companyId="${this.companyId}"></company-slider>
        </div>
      ` : ''}

      ${this.company!.hasCommunities ? html`
        <div class="slider-container" style="margin-top: 2rem;">
          <h2 style="font-size: 1.25rem; margin-bottom: 1rem; color: #333; text-align: center;">Communities</h2>
          <community-slider .companyId="${this.companyId}"></community-slider>
        </div>
      ` : ''}
    `;
  }

  private _renderOfferingsTab() {
    const offeringRefs = (this.company!.offerings as any)?.items || []; // Assume items has offering refs
    return html`
      <layout-card class="tab-section" heading="${this.company!.offerings?.label || 'What We Do'}">
        <div class="rich-text">
          ${this.company!.offerings?.multiOffering ? unsafeHTML(this.company!.offerings.multiOffering) : ''}
        </div>
        ${offeringRefs.length > 0 ? html`
          <offering-slider .companyId="${this.companyId}" .offeringRefs="${offeringRefs}"></offering-slider>
        ` : ''}
      </layout-card>
    `;
  }

  private _renderArticlesTab() {
    return html`
      <layout-card class="tab-section" heading="${this.company!.articles?.label || 'Articles'}">
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
