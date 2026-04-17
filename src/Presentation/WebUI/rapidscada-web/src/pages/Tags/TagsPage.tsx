import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Tag, Edit2 } from 'lucide-react';
import { tagService } from '../../services/api';
import { useTagStore } from '../../stores/tagStore';
import { signalrService } from '../../services/signalrService';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';

export default function TagsPage() {
  const { t } = useTranslation();
  const tags = useTagStore((state) => state.tags);
  const setTags = useTagStore((state) => state.setTags);
  
  const { data, isLoading } = useQuery({
    queryKey: ['tags'],
    queryFn: () => tagService.getAll(),
  });

  useEffect(() => {
    if (data?.data) {
      setTags(data.data);
      // Subscribe to all tags for real-time updates
      const tagIds = data.data.map((t: any) => t.id);
      signalrService.subscribeToTags(tagIds);
    }
  }, [data, setTags]);

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-100">{t('tags.title')}</h1>
        <p className="text-gray-400 mt-1">Real-time tag values and monitoring</p>
      </div>

      <div className="glass-card overflow-hidden">
        <table className="table">
          <thead>
            <tr>
              <th>{t('tags.tagNumber')}</th>
              <th>{t('tags.tagName')}</th>
              <th>{t('tags.device')}</th>
              <th>{t('tags.currentValue')}</th>
              <th>{t('tags.quality')}</th>
              <th>{t('common.timestamp')}</th>
              <th>{t('common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {tags.map((tag: any) => (
              <tr key={tag.id}>
                <td className="font-mono">{tag.tagNumber}</td>
                <td className="flex items-center gap-2">
                  <Tag className="w-4 h-4 text-purple-400" />
                  {tag.name}
                </td>
                <td>{tag.deviceName || '-'}</td>
                <td>
                  <span className="value-display value-good">
                    {tag.currentValue !== undefined ? tag.currentValue : '-'}
                    {tag.unit && <span className="text-gray-500 ml-1">{tag.unit}</span>}
                  </span>
                </td>
                <td>
                  <span className={tag.quality === 1.0 ? 'text-green-400' : 'text-red-400'}>
                    {((tag.quality || 0) * 100).toFixed(0)}%
                  </span>
                </td>
                <td className="text-sm text-gray-400">
                  {tag.timestamp ? new Date(tag.timestamp).toLocaleTimeString() : '-'}
                </td>
                <td>
                  {tag.isWritable && (
                    <button className="btn btn-secondary text-sm px-2 py-1">
                      <Edit2 className="w-4 h-4" />
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
