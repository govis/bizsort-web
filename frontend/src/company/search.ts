import { LitElement, html, css } from 'lit';
import { property, state } from 'lit/decorators.js';
import { setBasePath } from '@awesome.me/webawesome/dist/utilities/base-path.js';
import { search, toPreview } from '../service/company';
import type { CompanyPreview } from '../components/types';

setBasePath('https://cdn.jsdelivr.net/npm/@awesome.me/webawesome@3.8.0/dist/');

import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '../components/company/card';
import './header-layout';

/**
 * Company Search page.
 * Ported from legacy company/search.ts.
 */
export class CompanySearch extends LitElement {
  static get properties() {
    return {
      query: { type: String },
      categoryId: { type: Number, attribute: 'category-id' },
      _results: { state: true },
      _loading: { state: true },
      _error: { state: true }
    };
  }

  declare query?: string;
  declare categoryId?: number;
  declare private _results: CompanyPreview[];
  declare private _loading: boolean;
  declare private _error?: string;

  constructor() {
    super();
    this._results = [];
    this._loading = false;
  }

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if (changedProperties.has('query') || changedProperties.has('categoryId')) {
      this._performSearch();
    }
  }

  private async _performSearch() {
    if (!this.query && !this.categoryId) {
      this._results = [];
      return;
    }

    this._loading = true;
    this._error = undefined;

    try {
      // Legacy structure: queryInput expects { index, length, searchQuery, transactionType, etc. }
      const searchOutput = await search({
        index: 0,
        length: 24,
        searchQuery: this.query || undefined,
        category: this.categoryId || undefined,
        transactionType: 1 // 1 is Company view in legacy
      });

      if (searchOutput && searchOutput.series && searchOutput.series.length > 0) {
        // Map to SearchItem objects (with id property)
        const items = searchOutput.series.map((item: any) => ({
           id: item.id || item,
           office: item.office || undefined
        }));

        this._results = await toPreview(items);
      } else {
        this._results = [];
      }
    } catch (e: unknown) {
      this._error = e instanceof Error ? e.message : 'Search failed';
      console.error(this._error);
      this._results = [];
    } finally {
      this._loading = false;
    }
  }

  static styles = css`
    :host {
      display: block;
      min-height: 100vh;
      background-color: #f5f5f5;
      font-family: Roboto, var(--wa-font-sans, sans-serif);
    }

    .content {
      padding: 2rem 1rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .results-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;
      justify-items: center;
    }

    .empty-state, .error-state, .loading-state {
      text-align: center;
      padding: 4rem 1rem;
      color: var(--wa-color-neutral-600);
      font-size: 1.1rem;
    }

    .error-state {
      color: var(--wa-color-danger-600);
    }

    .page-header {
      margin-bottom: 2rem;
    }

    .page-header h1 {
      font-size: 1.8rem;
      color: #1a237e;
      margin: 0 0 0.5rem 0;
    }

    .page-header p {
      color: var(--wa-color-neutral-600);
      margin: 0;
    }
  `;

  render() {
    return html`
      <company-header-layout title-text="Search Results">
        <div slot="logo" style="display: flex; align-items: center; height: 100%; justify-content: center; color: white; font-weight: bold; font-size: 1.2rem;">
          bizSORT
        </div>
        
        <search-box slot="navbar" query="\${this.query || ''}"></search-box>

        <div class="content">
          <div class="page-header">
            <h1>Search Results</h1>
            <p>\${this.query ? \`Showing results for "\${this.query}"\` : (this.categoryId ? \`Showing results for category\` : 'Enter a search term')}</p>
          </div>

          \${this._loading 
            ? html\`<div class="loading-state"><wa-spinner></wa-spinner> Loading results...</div>\`
            : this._error
              ? html\`<div class="error-state">Error: \${this._error}</div>\`
              : this._results.length > 0
                ? html\`
                    <div class="results-grid">
                      \${this._results.map(company => html\`
                        <company-card .model="\${company}"></company-card>
                      \`)}
                    </div>
                  \`
                : (this.query || this.categoryId) ? html\`<div class="empty-state">No companies found matching your criteria.</div>\` : ''
          }
        </div>
      </company-header-layout>
    `;
  }
}

if (!customElements.get('company-search')) {
  customElements.define('company-search', CompanySearch);
}
