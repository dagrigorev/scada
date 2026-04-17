import { create } from 'zustand';
import { Tag, TagUpdate } from '../types';

interface TagState {
  tags: Tag[];
  setTags: (tags: Tag[]) => void;
  updateTagValues: (updates: TagUpdate[]) => void;
  addTag: (tag: Tag) => void;
  removeTag: (id: number) => void;
}

export const useTagStore = create<TagState>((set) => ({
  tags: [],
  setTags: (tags) => set({ tags }),
  updateTagValues: (updates) =>
    set((state) => ({
      tags: state.tags.map((tag) => {
        const update = updates.find((u) => u.tagId === tag.id);
        if (update) {
          return {
            ...tag,
            currentValue: update.value,
            quality: update.quality,
            timestamp: update.timestamp,
          };
        }
        return tag;
      }),
    })),
  addTag: (tag) => set((state) => ({ tags: [...state.tags, tag] })),
  removeTag: (id) => set((state) => ({ tags: state.tags.filter((t) => t.id !== id) })),
}));
