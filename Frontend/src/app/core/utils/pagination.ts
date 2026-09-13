import type { Paginated } from '../models/paginated';

export const MAX_PAGE_SIZE = 100;

export async function fetchAllPages<T>(loader: (page: number, pageSize: number) => Promise<Paginated<T>>): Promise<T[]> {
  const items: T[] = [];
  let page = 1;
  let totalCount: number | null = null;

  while (totalCount === null || items.length < totalCount) {
    const result = await loader(page, MAX_PAGE_SIZE);
    items.push(...result.items);
    totalCount = result.totalCount;
    if (!result.hasNextPage || result.items.length === 0) {
      break;
    }
    page += 1;
  }

  return items;
}