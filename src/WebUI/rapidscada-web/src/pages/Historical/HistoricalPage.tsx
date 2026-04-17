import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Calendar, Download } from 'lucide-react';
import { tagService, historicalService } from '../../services/api';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

export default function HistoricalPage() {
  const { t } = useTranslation();
  
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [startDate, setStartDate] = useState(
    new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString().slice(0, 16)
  );
  const [endDate, setEndDate] = useState(
    new Date().toISOString().slice(0, 16)
  );

  const { data: tagsData, isLoading: tagsLoading } = useQuery({
    queryKey: ['tags'],
    queryFn: () => tagService.getAll(),
  });

  const { data: historicalData, isLoading: historicalLoading, refetch } = useQuery({
    queryKey: ['historical', selectedTagIds, startDate, endDate],
    queryFn: () => {
      if (selectedTagIds.length === 0) return null;
      return historicalService.getMultipleTagsHistory(selectedTagIds, startDate, endDate);
    },
    enabled: selectedTagIds.length > 0,
  });

  const tags = tagsData?.data || [];

  const handleTagToggle = (tagId: number) => {
    setSelectedTagIds(prev =>
      prev.includes(tagId) ? prev.filter(id => id !== tagId) : [...prev, tagId]
    );
  };

  const handleLoadData = () => {
    refetch();
  };

  const formatChartData = () => {
    if (!historicalData?.data) return [];

    const dataByTimestamp: { [key: string]: any } = {};

    historicalData.data.forEach((point: any) => {
      const timestamp = new Date(point.timestamp).toLocaleString();
      if (!dataByTimestamp[timestamp]) {
        dataByTimestamp[timestamp] = { timestamp };
      }
      const tag = tags.find((t: any) => t.id === point.tagId);
      if (tag) {
        dataByTimestamp[timestamp][tag.name] = point.value;
      }
    });

    return Object.values(dataByTimestamp);
  };

  const chartData = formatChartData();

  if (tagsLoading) return <LoadingSpinner />;

  const colors = ['#0ea5e9', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-100">{t('historical.title')}</h1>
        <p className="text-gray-400 mt-1">View and analyze historical tag data</p>
      </div>

      {/* Controls */}
      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold text-gray-100 mb-4">Query Parameters</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
          {/* Start Date */}
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">
              Start Date & Time
            </label>
            <input
              type="datetime-local"
              className="input"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
            />
          </div>

          {/* End Date */}
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">
              End Date & Time
            </label>
            <input
              type="datetime-local"
              className="input"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
            />
          </div>

          {/* Load Button */}
          <div className="flex items-end">
            <button
              onClick={handleLoadData}
              className="btn btn-primary w-full"
              disabled={selectedTagIds.length === 0}
            >
              <Calendar className="w-5 h-5 mr-2" />
              Load Data
            </button>
          </div>
        </div>

        {/* Tag Selection */}
        <div>
          <label className="block text-sm font-medium text-gray-300 mb-2">
            Select Tags to Display
          </label>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
            {tags.slice(0, 8).map((tag: any) => (
              <label
                key={tag.id}
                className="flex items-center gap-2 p-2 bg-gray-800 rounded cursor-pointer hover:bg-gray-700"
              >
                <input
                  type="checkbox"
                  checked={selectedTagIds.includes(tag.id)}
                  onChange={() => handleTagToggle(tag.id)}
                  className="rounded"
                />
                <span className="text-sm text-gray-300">{tag.name}</span>
              </label>
            ))}
          </div>
        </div>
      </div>

      {/* Chart */}
      {selectedTagIds.length > 0 && (
        <div className="glass-card p-6">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-lg font-semibold text-gray-100">Historical Data Chart</h2>
            <button className="btn btn-secondary text-sm">
              <Download className="w-4 h-4 mr-2" />
              Export CSV
            </button>
          </div>

          {historicalLoading ? (
            <LoadingSpinner />
          ) : chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height={400}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                <XAxis 
                  dataKey="timestamp" 
                  stroke="#9ca3af"
                  fontSize={12}
                />
                <YAxis stroke="#9ca3af" />
                <Tooltip
                  contentStyle={{
                    backgroundColor: '#1f2937',
                    border: '1px solid #374151',
                    borderRadius: '8px',
                  }}
                />
                <Legend />
                {tags
                  .filter((tag: any) => selectedTagIds.includes(tag.id))
                  .map((tag: any, index: number) => (
                    <Line
                      key={tag.id}
                      type="monotone"
                      dataKey={tag.name}
                      stroke={colors[index % colors.length]}
                      strokeWidth={2}
                      dot={false}
                    />
                  ))}
              </LineChart>
            </ResponsiveContainer>
          ) : (
            <div className="text-center py-12 text-gray-400">
              <Calendar className="w-16 h-16 mx-auto mb-4 opacity-50" />
              <p>No historical data available for selected period</p>
              <p className="text-sm mt-2">Try adjusting the date range</p>
            </div>
          )}
        </div>
      )}

      {/* Statistics */}
      {chartData.length > 0 && (
        <div className="glass-card p-6">
          <h2 className="text-lg font-semibold text-gray-100 mb-4">Statistics</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <p className="text-sm text-gray-400">Data Points</p>
              <p className="text-2xl font-bold text-gray-100">{chartData.length}</p>
            </div>
            <div>
              <p className="text-sm text-gray-400">Time Range</p>
              <p className="text-2xl font-bold text-gray-100">
                {Math.round((new Date(endDate).getTime() - new Date(startDate).getTime()) / 3600000)}h
              </p>
            </div>
            <div>
              <p className="text-sm text-gray-400">Tags Selected</p>
              <p className="text-2xl font-bold text-gray-100">{selectedTagIds.length}</p>
            </div>
            <div>
              <p className="text-sm text-gray-400">Interval</p>
              <p className="text-2xl font-bold text-gray-100">Auto</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
