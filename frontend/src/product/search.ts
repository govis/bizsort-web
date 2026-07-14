import { LitElement, html, css } from 'lit';
import { state } from 'lit/decorators.js';
import { search, toPreview } from '../service/product';
import { Filterable, Searchview } from '../viewmodel/list/view';
import type { IViewAdapter } from '../viewmodel';
import type { Action } from '../global';

import '@awesome.me/webawesome/dist/components/spinner/spinner.js';
import '../components/product/card';
import '../components/product/listview';
import '../components/list/pager';
import '../components/list/header';
import '../components/list/filter';
import '../components/directory/header-layout';

class ProductSearchViewModel extends Filterable(Searchview as any) {
  fetchList(queryInput: any, callback: Action<any>, faultCallback: Action<any>) {
    if (!this.searchParams) {
      faultCallback(new Error('No search params'));
      return;
    }

    // Build the final queryInput with search params
    queryInput.category = (this.searchParams as any).categoryId || 0;
    if ((this.searchParams as any).searchQuery)
      queryInput.searchQuery = (this.searchParams as any).searchQuery;
    if ((this.searchParams as any).productType)
      queryInput.productType = (this.searchParams as any).productType || 0;

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
      callback(data);
    }).catch(faultCallback);
  }

  fetchPage(page: any[], callback: Action<Object[]>, faultCallback: Action<any>) {
    const items = page.map((item: any) => ({
      id: item.id || item,
      office: item.office || undefined
    }));

    toPreview(items).then(results => {
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
}


/**
 * Product Search page.
 * Ported from legacy company/product.ts.
 */
export class ProductSearch extends LitElement implements IViewAdapter {
  static get properties() {
    return {
      searchQuery: { type: String, attribute: 'search-query' },
      categoryId: { type: Number, attribute: 'category-id' },
      locationId: { type: Number, attribute: 'location-id' },
      searchNear: { type: String, attribute: 'search-near' },
      productType: { type: Number, attribute: 'product-type' }
    };
  }

  declare searchQuery?: string;
  declare categoryId?: number;
  declare locationId?: number;
  declare searchNear?: string;
  declare productType?: number;
  
  @state()
  declare _errorText?: string;
  
  private _items: any[] = [];
  set items(val: any[]) {
    this._items = val;
    this.requestUpdate();
  }
  get items() { return this._items; }
  
  setItemOption(name: string, option: any) {
    const listview = this.shadowRoot?.querySelector('product-listview') as any;
    if (listview && listview.viewModel) {
      listview.viewModel.setItemOption(name, option);
    }
  }
  
  viewModel: ProductSearchViewModel;

  constructor() {
    super();
    // @ts-expect-error
    this.viewModel = new ProductSearchViewModel(this);
    this.viewModel.pager.pageSize = 24; 
  }

  firstUpdated() {
    this.viewModel.initialize({
      listView: this as any
    });
    
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
      productType: this.productType
    } as any;
  }

  willUpdate(changedProperties: Map<string | number | symbol, unknown>) {
    if (changedProperties.has('searchQuery') || changedProperties.has('categoryId') || 
        changedProperties.has('locationId') || changedProperties.has('searchNear') || 
        changedProperties.has('productType')) {
        
      if (!this.categoryId && !this.searchQuery) {
        this._errorText = "Invalid Search: Please provide either a category or a search query to continue.";
        return;
      }
      this._errorText = undefined;
      
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
  `;

  render() {
    return html`
      <directory-header-layout 
        entity-type="product"
        .categoryId=${this.categoryId}
        .locationId=${this.locationId}
        .searchQuery=${this.searchQuery}
        .searchNear=${this.searchNear}
      >
        <div class="content">
          <div class="page-header" style="display: flex; justify-content: space-between; align-items: flex-end;">
            <div>
              <h1>Product Search Results</h1>
              <list-header entity="products"></list-header>
            </div>
            <list-filter-available></list-filter-available>
          </div>
          <list-filter-applied style="margin-bottom: 1rem;"></list-filter-applied>

          <product-listview .items="${this._items}"></product-listview>
          
          ${this.isLoading 
            ? html`<div class="loading-state">Loading products...</div>` 
            : this._errorText 
                ? html`<div class="error-state">Error: ${this._errorText}</div>`
                : this._items.length > 0
                  ? html`<list-pager .master="${this.viewModel.pager}"></list-pager>`
                  : html`<div class="empty-state">No products found matching your search.</div>`
          }
        </div>
      </directory-header-layout>
    `;
  }
}

if (!customElements.get('product-search')) {
  customElements.define('product-search', ProductSearch);
}
