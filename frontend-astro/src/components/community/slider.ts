import { html, css, LitElement } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { getCommunities } from '../../service/company.js';
import { toPreview } from '../../service/community.js';
import '../layout/list-slider.js';
import '@awesome.me/webawesome/dist/components/icon/icon.js';

@customElement('community-slider')
export class CommunitySlider extends LitElement {
  @property({ type: Number }) declare companyId: number;

  @state() declare private _previews: any[];
  @state() declare private _loading: boolean;
  @state() declare private _hasMore: boolean;
  
  private _nextIndex = 0;

  constructor() {
    super();
    this.companyId = 0;
    this._previews = [];
    this._loading = false;
    this._hasMore = true;
  }

  async loadMore() {
    if (!this.companyId || this._loading || !this._hasMore) return;
    
    this._loading = true;
    try {
      const data = await getCommunities(this.companyId, this._nextIndex, 4);
      
      let previews: any[] = [];
      if (data && data.series && data.series.length > 0) {
        previews = await toPreview(data.series);
      }

      this._previews = [...this._previews, ...previews];
      
      if (data.nextIndex !== -1 && previews.length > 0) {
        this._nextIndex = data.nextIndex;
      } else {
        this._hasMore = false;
      }
    } catch (e) {
      console.error('Failed to load communities:', e);
      this._hasMore = false;
    } finally {
      this._loading = false;
    }
  }

  render() {
    if (this._previews.length === 0 && !this._loading) return html``;

    return html`
      <list-slider
        title="Communities"
        .items=${this._previews}
        .hasMore=${this._hasMore}
        .loading=${this._loading}
        @load-more=${this.loadMore}
      >
        ${this._previews.map(item => html`
          <a class="community-card" href="/community/${item.id}">
            <div class="image-placeholder">
              ${item.image && item.image.imageId > 0 ? html`
                <img src="/api/image/get?entity=2&id=${item.image.imageId}&width=200&height=200" alt="${item.name}" loading="lazy" />
              ` : html`
                <wa-icon name="users" library="system"></wa-icon>
              `}
            </div>
            <strong>${item.name || `Community ${item.id}`}</strong>
          </a>
        `)}
      </list-slider>
    `;
  }

  static styles = [
    css`
      .community-card {
        scroll-snap-align: start;
        flex-shrink: 0;
        width: 260px;
        padding: 1rem;
        background: var(--wa-color-neutral-fill-normal);
        border-radius: 8px;
        color: var(--wa-color-neutral-text);
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        align-items: center;
        text-align: center;
        cursor: pointer;
        animation: card-enter 500ms cubic-bezier(0.4, 0, 0.2, 1) both;
      }

      .card:hover {
        background: var(--wa-color-neutral-fill-hover);
      }

      @keyframes card-enter {
        from {
          opacity: 0;
          transform: translateY(40px) scale(0.95);
        }
        to {
          opacity: 1;
          transform: translateY(0) scale(1);
        }
      }

      .card:nth-child(4n + 1) { animation-delay: 0ms; }
      .card:nth-child(4n + 2) { animation-delay: 75ms; }
      .card:nth-child(4n + 3) { animation-delay: 150ms; }
      .card:nth-child(4n + 4) { animation-delay: 225ms; }
    `
  ];
}

if (!customElements.get('community-slider')) {
  customElements.define('community-slider', CommunitySlider);
}
