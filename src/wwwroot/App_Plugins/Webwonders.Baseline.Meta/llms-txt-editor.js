import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
// Registers <umb-input-multi-url>, the same link picker the Multi URL Picker property editor uses.
import '@umbraco-cms/backoffice/multi-url-picker';

const uid = () => (crypto.randomUUID ? crypto.randomUUID() : String(Date.now() + Math.random()));

/**
 * Property editor for llms.txt content, styled like the Block List editor.
 * Value shape:
 * { title, description, chapters: [{ id, title, items: [{ id, link: { name, url, unique, type, queryString, target }, description }] }] }
 */
export class WebwondersLlmsTxtEditor extends UmbLitElement {
  static properties = {
    _model: { state: true },
    _open: { state: true },
    _drag: { state: true },
  };

  constructor() {
    super();
    this._model = { title: '', description: '', chapters: [] };
    this._open = {};
    // Drag state: { from: index being dragged, over: index hovered, after: drop below the hovered block }
    this._drag = null;
  }

  // Only the handle starts a drag, so text selection and inputs inside a chapter keep working.
  #armDrag(e) {
    e.currentTarget.closest('.block').draggable = true;
  }

  #disarmDrag(e) {
    const block = e.currentTarget.closest('.block');
    if (block) block.draggable = false;
  }

  #dragStart(e, ci) {
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', String(ci)); // required by Firefox
    this._drag = { from: ci, over: ci, after: false };
  }

  #dragOver(e, ci) {
    if (!this._drag) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    const rect = e.currentTarget.getBoundingClientRect();
    const after = e.clientY > rect.top + rect.height / 2;
    if (this._drag.over !== ci || this._drag.after !== after) {
      this._drag = { ...this._drag, over: ci, after };
    }
  }

  #drop(e) {
    e.preventDefault();
    const d = this._drag;
    this._drag = null;
    if (!d) return;
    // Index the dragged chapter lands at once it has been removed from its old position.
    let to = d.over + (d.after ? 1 : 0);
    if (d.from < to) to--;
    if (to === d.from) return;
    this.#update((m) => {
      const [c] = m.chapters.splice(d.from, 1);
      m.chapters.splice(to, 0, c);
    });
  }

  #dragEnd(e) {
    e.currentTarget.draggable = false;
    this._drag = null;
  }

  get value() {
    return this._model;
  }

  set value(v) {
    const m = v && typeof v === 'object' ? JSON.parse(JSON.stringify(v)) : {};
    this._model = {
      title: m.title ?? '',
      description: m.description ?? '',
      chapters: (Array.isArray(m.chapters) ? m.chapters : []).map((c) => ({
        id: c.id ?? uid(),
        title: c.title ?? '',
        items: (Array.isArray(c.items) ? c.items : []).map((i) => ({
          id: i.id ?? uid(),
          // Older values stored a plain name/url pair; keep them visible as an external link.
          link: i.link ?? (i.url ? { name: i.name ?? '', url: i.url, type: 'external' } : null),
          description: i.description ?? '',
        })),
      })),
    };
  }

  // Umbraco may hand back frozen/shared objects, so never mutate the current model: edit a deep copy.
  #update(fn) {
    const copy = JSON.parse(JSON.stringify(this._model));
    fn(copy);
    this._model = copy;
    this.dispatchEvent(new UmbChangeEvent());
  }

  #toggle(id) {
    this._open = { ...this._open, [id]: !this._open[id] };
  }

  #addChapter() {
    const id = uid();
    this._open = { ...this._open, [id]: true };
    this.#update((m) => m.chapters.push({ id, title: '', items: [] }));
  }

  #removeChapter(ci) {
    this.#update((m) => m.chapters.splice(ci, 1));
  }

  #addItem(ci) {
    this.#update((m) => m.chapters[ci].items.push({ id: uid(), link: null, description: '' }));
  }

  #removeItem(ci, ii) {
    this.#update((m) => m.chapters[ci].items.splice(ii, 1));
  }

  #moveItem(ci, ii, delta) {
    this.#update((m) => {
      const items = m.chapters[ci].items;
      const target = ii + delta;
      if (target < 0 || target >= items.length) return;
      const [i] = items.splice(ii, 1);
      items.splice(target, 0, i);
    });
  }

  #renderItem(ci, item, ii, count) {
    return html`
      <div class="item">
        <div class="item-fields">
          <umb-input-multi-url
            .urls=${item.link ? [item.link] : []}
            .min=${0}
            .max=${1}
            @change=${(e) => {
              const link = e.target.urls?.[0] ?? null;
              this.#update((m) => { m.chapters[ci].items[ii].link = link; });
            }}></umb-input-multi-url>
          <uui-input label="Description" placeholder="Short description (optional)" .value=${item.description ?? ''}
            @input=${(e) => { const v = e.target.value; this.#update((m) => { m.chapters[ci].items[ii].description = v; }); }}>
          </uui-input>
        </div>
        <div class="item-actions">
          <uui-button compact look="outline" label="Move up" ?disabled=${ii === 0} @click=${() => this.#moveItem(ci, ii, -1)}>
            <uui-icon name="icon-arrow-up"></uui-icon></uui-button>
          <uui-button compact look="outline" label="Move down" ?disabled=${ii === count - 1} @click=${() => this.#moveItem(ci, ii, 1)}>
            <uui-icon name="icon-arrow-down"></uui-icon></uui-button>
          <uui-button compact look="outline" color="danger" label="Remove link" @click=${() => this.#removeItem(ci, ii)}>
            <uui-icon name="icon-trash"></uui-icon></uui-button>
        </div>
      </div>`;
  }

  #renderChapter(chapter, ci, count) {
    const open = !!this._open[chapter.id];
    const items = chapter.items ?? [];
    const d = this._drag;
    const dragging = d && d.from === ci;
    const drop = d && d.over === ci && d.from !== ci ? (d.after ? 'drop-after' : 'drop-before') : '';
    return html`
      <div class="block ${open ? 'open' : ''} ${dragging ? 'dragging' : ''} ${drop}"
        @dragstart=${(e) => this.#dragStart(e, ci)}
        @dragover=${(e) => this.#dragOver(e, ci)}
        @drop=${(e) => this.#drop(e)}
        @dragend=${(e) => this.#dragEnd(e)}>
        <div class="block-head" @click=${() => this.#toggle(chapter.id)}>
          <uui-icon name="icon-bookmark"></uui-icon>
          <div class="block-name">
            <strong>${chapter.title || 'Untitled chapter'}</strong>
            <small>${items.length} ${items.length === 1 ? 'link' : 'links'}</small>
          </div>
          <div class="block-actions" @click=${(e) => e.stopPropagation()}>
            <uui-button class="handle" compact look="outline" label="Drag to reorder" title="Drag to reorder"
              @mousedown=${(e) => this.#armDrag(e)} @mouseup=${(e) => this.#disarmDrag(e)}>
              <uui-icon name="icon-navigation"></uui-icon></uui-button>
            <uui-button compact look="outline" color="danger" label="Remove chapter" @click=${() => this.#removeChapter(ci)}>
              <uui-icon name="icon-trash"></uui-icon></uui-button>
          </div>
        </div>
        ${open
          ? html`
              <div class="block-body">
                <uui-input label="Chapter title" placeholder="Chapter title, e.g. Products" .value=${chapter.title ?? ''}
                  @input=${(e) => { const v = e.target.value; this.#update((m) => { m.chapters[ci].title = v; }); }}></uui-input>
                ${items.map((item, ii) => this.#renderItem(ci, item, ii, items.length))}
                <uui-button look="placeholder" label="Add link" @click=${() => this.#addItem(ci)}></uui-button>
              </div>`
          : nothing}
      </div>`;
  }

  render() {
    const chapters = this._model.chapters;
    return html`
      <div class="site">
        <uui-input label="Title" placeholder="Site name" .value=${this._model.title}
          @input=${(e) => { const v = e.target.value; this.#update((m) => { m.title = v; }); }}></uui-input>
        <uui-textarea label="Description" placeholder="One or two sentences summarising the site"
          .value=${this._model.description}
          @input=${(e) => { const v = e.target.value; this.#update((m) => { m.description = v; }); }}></uui-textarea>
      </div>
      ${chapters.map((c, i) => this.#renderChapter(c, i, chapters.length))}
      <uui-button look="placeholder" label="Add chapter" @click=${() => this.#addChapter()}></uui-button>`;
  }

  static styles = css`
    :host { display: flex; flex-direction: column; gap: var(--uui-size-space-3); }
    uui-input, uui-textarea { width: 100%; }
    .site { display: flex; flex-direction: column; gap: var(--uui-size-space-2); }
    .block {
      background: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
    }
    .block.open { border-color: var(--uui-color-border-emphasis); }
    .block.dragging { opacity: 0.4; }
    .block.drop-before { box-shadow: 0 -3px 0 0 var(--uui-color-interactive-emphasis); }
    .block.drop-after { box-shadow: 0 3px 0 0 var(--uui-color-interactive-emphasis); }
    .handle { cursor: grab; }
    .handle:active { cursor: grabbing; }
    .block-head {
      display: flex; align-items: center; gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-3) var(--uui-size-space-4); cursor: pointer;
    }
    .block-head:hover { background: var(--uui-color-surface-emphasis); }
    .block-name { flex: 1; display: flex; flex-direction: column; min-width: 0; }
    .block-name small { color: var(--uui-color-text-alt); }
    .block-actions, .item-actions { display: flex; gap: var(--uui-size-space-1); }
    .block-body {
      display: flex; flex-direction: column; gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-4); border-top: 1px solid var(--uui-color-border);
    }
    .item {
      display: flex; gap: var(--uui-size-space-3); align-items: flex-start;
      padding: var(--uui-size-space-3); background: var(--uui-color-surface-alt);
      border-radius: var(--uui-border-radius);
    }
    .item-fields { flex: 1; display: flex; flex-direction: column; gap: var(--uui-size-space-2); min-width: 0; }
  `;
}

customElements.define('webwonders-llms-txt-editor', WebwondersLlmsTxtEditor);

export default WebwondersLlmsTxtEditor;
