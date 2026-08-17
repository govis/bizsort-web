import { LitElement, html, css } from 'lit';
import { state } from 'lit/decorators.js';
import { search, toPreview } from '../service/company';
import { Filterable, Searchview } from '../viewmodel/list/view';
import type { IViewAdapter } from '../viewmodel';
import type { Action } from '../global';

import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '../components/company/card';
import '../components/company/listview';
import '../components/list/pager';
import '../components/list/header';
import '../components/list/filter';
import '../components/list/pager'; // ensure it's imported
import '../components/directory/header-layout';

class CompanySearchViewModel extends Filterable(Searchview as any) {
  fetchList(queryInput: any, callback: Action<any>, faultCallback: Action<any>) {
    console.log('[CompanySearch] fetchList called, searchParams=', this.searchParams, 'queryInput=', queryInput);
    console.log('[CompanySearch] _filterApplied=', (this as any)._filterApplied, '_filterAvail=', (this as any)._filterAvail);

    if (!this.searchParams) {
      faultCallback(new Error('No search params'));
      return;
    }

    // Build the final queryInput with search params (mirrors what Searchview.fetchList does)
    queryInput.category = (this.searchParams as any).categoryId || 0;
    if ((this.searchParams as any).searchQuery)
      queryInput.searchQuery = (this.searchParams as any).searchQuery;
    if ((this.searchParams as any).transactionType !== undefined && (this.searchParams as any).transactionType !== 3)
      queryInput.transactionType = (this.searchParams as any).transactionType || 0;

    if ((this.searchParams as any).searchNear) {
      try {
        let nearVal = (this.searchParams as any).searchNear;
        if (typeof nearVal === 'string') {
           // Handle case where it might be URI encoded
           if (nearVal.startsWith('%7B')) nearVal = decodeURIComponent(nearVal);
           nearVal = JSON.parse(nearVal);
        }
        queryInput.searchNear = nearVal;
      } catch (e) {
        // Fallback to undefined if parsing fails completely so we don't crash C# backend with a string
        queryInput.searchNear = undefined;
      }
    } else {
      queryInput.location = (this.searchParams as any).locationId || 0;
    }

    // Bridge async service function → callback pattern
    search(queryInput).then((data: any) => {
      console.log('[CompanySearch] fetchList callback data=', data, 'series.length=', data?.series?.length);
      callback(data);
    }).catch(faultCallback);
  }

  fetchPage(page: any[], callback: Action<Object[]>, faultCallback: Action<any>) {
    console.log('[CompanySearch] fetchPage called, page=', page);
    const items = page.map((item: any) => ({
      id: item.id || item,
      office: item.office || undefined
    }));

    toPreview(items).then(results => {
      console.log('[CompanySearch] toPreview results=', results);
      if (this.searchParams?.searchNear && results) {
        results.forEach((r: any, i: number) => {
          const ref = page[i];
          if (ref && ref.distance !== undefined) {
            r.distance = `${ref.distance}km`;
          }
        });
      }
      callback(this.preparePage(page, results));
    }).catch(faultCallback);
  }

  populateHeader(header: any) {
    if (this.searchParams && (this.searchParams as any).searchQuery) {
      header.query = (this.searchParams as any).searchQuery;
    }
    return header;
  }
}

/**
 * Company Search page.
 * Ported from legacy company/search.ts.
 */
export class CompanySearch extends LitElement implements IViewAdapter {
  static get properties() {
    return {
      searchQuery: { type: String, attribute: 'search-query' },
      categoryId: { type: Number, attribute: 'category-id' },
      locationId: { type: Number, attribute: 'location-id' },
      searchNear: { type: String, attribute: 'search-near' },
      transactionType: { type: Number, attribute: 'transaction-type' }
    };
  }

  declare searchQuery?: string;
  declare categoryId?: number;
  declare locationId?: number;
  declare searchNear?: string;
  declare transactionType?: number;
  
  @state()
  declare _errorText?: string;
  
  // ListView stub properties
  private _items: any[] = [];
  set items(val: any[]) {
    this._items = val;
    this.requestUpdate();
  }
  get items() { return this._items; }
  
  setItemOption(name: string, option: any) {
    const listview = this.shadowRoot?.querySelector('company-listview') as any;
    if (listview && listview.viewModel) {
      listview.viewModel.setItemOption(name, option);
    }
  }
  
  viewModel: CompanySearchViewModel;

    constructor() {
    super();
    // @ts-expect-error
    this.viewModel = new CompanySearchViewModel(this);
    this.viewModel.pager.pageSizes = [20, 50, 100]; 
  }

  connectedCallback() {
    super.connectedCallback();
    // In Lit, we don't initialize DOM-dependent viewModels here because shadowRoot is empty.
  }

  firstUpdated() {
    console.log('[CompanySearch] firstUpdated, categoryId=', this.categoryId, 'searchQuery=', this.searchQuery);
    console.log('[CompanySearch] filterAvail el=', this.shadowRoot?.querySelector('list-filter-available'));
    console.log('[CompanySearch] filterApplied el=', this.shadowRoot?.querySelector('list-filter-applied'));
    // Now that the shadow DOM is rendered, we can initialize the ViewModel 
    // so it can find 'list-header', 'list-filter-available', etc.
    this.viewModel.initialize({
      listView: this as any
    });
    console.log('[CompanySearch] after initialize, _filterApplied=', (this.viewModel as any)._filterApplied);
    
    // Trigger the initial search now that filters are wired up
    if (this.categoryId || this.searchQuery) {
      this._updateSearchParams();
      this.viewModel.search();
    }
  }

  modelUpdated(props: string[]) {
    this.requestUpdate();
  }

  getViewModel(name: string) {
    if (name === 'listView') return this;
    if (name === 'listHeader') return this.shadowRoot?.querySelector('list-header');
    if (name === 'filterAvail') return (this.shadowRoot?.querySelector('list-filter-available') as any)?.viewModel;
    if (name === 'filterApplied') return (this.shadowRoot?.querySelector('list-filter-applied') as any)?.viewModel;
    return null;
  }

  private _updateSearchParams() {
    this.viewModel.searchParams = {
      categoryId: this.categoryId,
      searchQuery: this.searchQuery,
      searchNear: this.searchNear,
      locationId: this.locationId,
      transactionType: this.transactionType
    } as any;
  }

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if (changedProperties.has('searchQuery') || changedProperties.has('categoryId') || 
        changedProperties.has('locationId') || changedProperties.has('searchNear') || 
        changedProperties.has('transactionType')) {
        
      console.log('[CompanySearch] willUpdate', this.categoryId, this.searchQuery);
      if (!this.categoryId && !this.searchQuery) {
        this._errorText = "Invalid Search: Please provide either a category or a search query to continue.";
        return;
      }
      this._errorText = undefined;
      
      // Only search if the component has already been initialized (post-firstUpdated)
      if (this.hasUpdated) {
        this._updateSearchParams();
        this.viewModel.search();
      }
    }
  }
  
  get isLoading() {
    return (this.viewModel as any)._fetchPending;
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
      display: flex;
      flex-direction: column;
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
      <directory-header-layout 
        entity-type="company"
        .categoryId=${this.categoryId}
        .locationId=${this.locationId}
        .searchQuery=${this.searchQuery}
        .searchNear=${this.searchNear}
      >
        <div class="content">
          <div class="page-header">
            <div class="list-header-container" style="display: flex; align-items: center; gap: 1rem; flex-wrap: wrap;">
              <list-filter-available></list-filter-available>
              <list-filter-applied></list-filter-applied>
              <list-header entity="companies"></list-header>
              <div style="margin-left: auto;">
                <list-page-select .pager=${this.viewModel.pager}></list-page-select>
              </div>
            </div>
          </div>

          <company-listview .items="${this._items}"></company-listview>
          
          ${this.isLoading 
            ? html`<div class="loading-state"><wa-spinner></wa-spinner> Loading results...</div>`
            : this._errorText
              ? html`<div class="error-state">Error: ${this._errorText}</div>`
              : this.viewModel.pager.itemCount > 0
                ? html`<list-pager .master="${this.viewModel.pager}"></list-pager>`
                : (this.searchQuery || this.categoryId) ? html`<div class="empty-state">No companies found matching your criteria.</div>` : ''
          }
        </div>
      </directory-header-layout>
    `;
  }
}

if (!customElements.get('company-search')) {
  customElements.define('company-search', CompanySearch);
}
