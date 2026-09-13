import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.html',
})
export class PageHeaderComponent {
  readonly title = input('');
  readonly subtitle = input('');
}