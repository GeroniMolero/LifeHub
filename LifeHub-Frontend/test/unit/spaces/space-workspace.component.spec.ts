import { FormBuilder } from '@angular/forms';
import { of } from 'rxjs';

import { SpaceWorkspaceComponent } from '../../../src/app/pages/spaces/space-workspace/space-workspace.component';

describe('SpaceWorkspaceComponent markdown security', () => {
  const createComponent = (): SpaceWorkspaceComponent => {
    return new SpaceWorkspaceComponent(
      {} as any,
      new FormBuilder(),
      { sanitize: (_context: any, value: string | null) => value } as any,
      { getEmbedAllowlist: () => of([]) } as any,
      {} as any,
      {} as any,
      {} as any,
      {} as any
    );
  };

  it('escapes raw HTML blocks', () => {
    const component = createComponent();

    const rendered = (component as any).renderMarkdownToHtml('<script>alert(1)</script>') as string;

    expect(rendered).toContain('&lt;script&gt;alert(1)&lt;/script&gt;');
    expect(rendered).not.toContain('<script>');
  });

  it('neutralizes javascript links', () => {
    const component = createComponent();

    const rendered = (component as any).renderMarkdownToHtml('[click](javascript:alert(1))') as string;

    expect(rendered).toContain('href="#"');
    expect(rendered).not.toContain('href="javascript:');
  });

  it('keeps fenced code rendering', () => {
    const component = createComponent();

    const rendered = (component as any).renderMarkdownToHtml('```js\nconsole.log(1)\n```') as string;

    expect(rendered).toContain('<pre><code class="language-js">');
    expect(rendered).toContain('console.log(1)');
  });

  it('renders checked task list items as ☑ symbols', () => {
    const component = createComponent();

    const rendered = (component as any).renderMarkdownToHtml('- [x] Tarea completada') as string;

    expect(rendered).toContain('☑');
    expect(rendered).toContain('task-item');
    expect(rendered).not.toContain('[x]');
  });

  it('renders unchecked task list items as ☐ symbols', () => {
    const component = createComponent();

    const rendered = (component as any).renderMarkdownToHtml('- [ ] Tarea pendiente') as string;

    expect(rendered).toContain('☐');
    expect(rendered).toContain('task-item');
    expect(rendered).not.toContain('[ ]');
  });
});
