import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Layers, Plus, Save, Eye } from 'lucide-react';

export default function MnemonicPage() {
  const { t } = useTranslation();
  const [isEditMode, setIsEditMode] = useState(true);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-100">{t('mnemonic.title')}</h1>
          <p className="text-gray-400 mt-1">Visual SCADA diagrams and process displays</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setIsEditMode(!isEditMode)}
            className="btn btn-secondary"
          >
            <Eye className="w-5 h-5 mr-2" />
            {isEditMode ? 'Preview Mode' : 'Edit Mode'}
          </button>
          <button className="btn btn-primary">
            <Save className="w-5 h-5 mr-2" />
            Save Diagram
          </button>
        </div>
      </div>

      {/* Mnemonic List */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="glass-card p-6">
          <h3 className="text-lg font-semibold text-gray-100 mb-4">Available Diagrams</h3>
          
          <div className="space-y-2">
            <div className="p-4 bg-gray-800 rounded-lg cursor-pointer hover:bg-gray-700 border-l-4 border-sky-500">
              <div className="flex items-center gap-3">
                <Layers className="w-5 h-5 text-sky-400" />
                <div>
                  <p className="font-medium text-gray-100">Main Process</p>
                  <p className="text-xs text-gray-400">Updated 2 hours ago</p>
                </div>
              </div>
            </div>

            <div className="p-4 bg-gray-800 rounded-lg cursor-pointer hover:bg-gray-700">
              <div className="flex items-center gap-3">
                <Layers className="w-5 h-5 text-purple-400" />
                <div>
                  <p className="font-medium text-gray-100">Tank Farm</p>
                  <p className="text-xs text-gray-400">Updated 5 hours ago</p>
                </div>
              </div>
            </div>

            <div className="p-4 bg-gray-800 rounded-lg cursor-pointer hover:bg-gray-700">
              <div className="flex items-center gap-3">
                <Layers className="w-5 h-5 text-green-400" />
                <div>
                  <p className="font-medium text-gray-100">Pumping Station</p>
                  <p className="text-xs text-gray-400">Updated yesterday</p>
                </div>
              </div>
            </div>

            <button className="w-full p-4 bg-gray-800 rounded-lg hover:bg-gray-700 border-2 border-dashed border-gray-600">
              <Plus className="w-5 h-5 mx-auto mb-1 text-gray-400" />
              <p className="text-sm text-gray-400">Create New Diagram</p>
            </button>
          </div>
        </div>

        {/* Canvas */}
        <div className="md:col-span-2 glass-card p-6">
          <div className="bg-gray-900 rounded-lg border-2 border-gray-700 aspect-video flex items-center justify-center">
            <div className="text-center">
              <Layers className="w-16 h-16 mx-auto mb-4 text-gray-600" />
              <p className="text-gray-400 mb-2">Mnemonic Diagram Editor</p>
              <p className="text-sm text-gray-500">
                Advanced visual editor for process diagrams
              </p>
              <p className="text-xs text-gray-600 mt-4">
                This feature requires additional implementation
              </p>
            </div>
          </div>

          {/* Toolbar */}
          {isEditMode && (
            <div className="mt-4 flex gap-2 p-3 bg-gray-800 rounded-lg">
              <button className="px-3 py-2 bg-gray-700 rounded hover:bg-gray-600 text-sm">
                Select
              </button>
              <button className="px-3 py-2 bg-gray-700 rounded hover:bg-gray-600 text-sm">
                Tank
              </button>
              <button className="px-3 py-2 bg-gray-700 rounded hover:bg-gray-600 text-sm">
                Pump
              </button>
              <button className="px-3 py-2 bg-gray-700 rounded hover:bg-gray-600 text-sm">
                Valve
              </button>
              <button className="px-3 py-2 bg-gray-700 rounded hover:bg-gray-600 text-sm">
                Pipe
              </button>
              <button className="px-3 py-2 bg-gray-700 rounded hover:bg-gray-600 text-sm">
                Text
              </button>
              <button className="px-3 py-2 bg-gray-700 rounded hover:bg-gray-600 text-sm">
                Tag
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
