import React, { useState, useMemo } from 'react';
import { Client, Lead } from '../types';
import { Search, X, Users, ChevronDown, ChevronUp, Check } from './Icons';

interface ClientSelectorModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSelect: (type: 'client' | 'lead', id: string, name: string) => void;
    clients: Client[];
    leads: Lead[];
}

type SortField = 'name' | 'email' | 'type' | 'company';
type SortDirection = 'asc' | 'desc';

const ClientSelectorModal: React.FC<ClientSelectorModalProps> = ({
    isOpen,
    onClose,
    onSelect,
    clients,
    leads
}) => {
    const [searchQuery, setSearchQuery] = useState('');
    const [filterType, setFilterType] = useState<'all' | 'client' | 'lead'>('all');
    const [sortField, setSortField] = useState<SortField>('name');
    const [sortDirection, setSortDirection] = useState<SortDirection>('asc');
    const [selectedId, setSelectedId] = useState<string | null>(null);
    const [selectedType, setSelectedType] = useState<'client' | 'lead'>('client');

    // Combine clients and leads into a single list
    const allParties = useMemo(() => {
        const clientList = clients.map(c => ({
            id: c.id,
            name: c.name,
            email: c.email || '',
            phone: c.phone || '',
            company: c.company || '',
            type: 'client' as const,
            clientNumber: c.clientNumber,
            status: c.status
        }));

        const leadList = leads.map(l => ({
            id: l.id,
            name: l.name,
            email: '',
            phone: '',
            company: '',
            type: 'lead' as const,
            clientNumber: undefined,
            status: l.status
        }));

        return [...clientList, ...leadList];
    }, [clients, leads]);

    // Filter and sort
    const filteredParties = useMemo(() => {
        let result = allParties;

        // Filter by type
        if (filterType !== 'all') {
            result = result.filter(p => p.type === filterType);
        }

        // Search filter
        if (searchQuery) {
            const query = searchQuery.toLowerCase();
            result = result.filter(p =>
                p.name.toLowerCase().includes(query) ||
                p.email.toLowerCase().includes(query) ||
                p.company.toLowerCase().includes(query) ||
                (p.clientNumber && p.clientNumber.toLowerCase().includes(query))
            );
        }

        // Sort
        result.sort((a, b) => {
            let aVal = '';
            let bVal = '';

            switch (sortField) {
                case 'name':
                    aVal = a.name.toLowerCase();
                    bVal = b.name.toLowerCase();
                    break;
                case 'email':
                    aVal = a.email.toLowerCase();
                    bVal = b.email.toLowerCase();
                    break;
                case 'type':
                    aVal = a.type;
                    bVal = b.type;
                    break;
                case 'company':
                    aVal = a.company.toLowerCase();
                    bVal = b.company.toLowerCase();
                    break;
            }

            if (sortDirection === 'asc') {
                return aVal.localeCompare(bVal);
            }
            return bVal.localeCompare(aVal);
        });

        return result;
    }, [allParties, filterType, searchQuery, sortField, sortDirection]);

    const handleSort = (field: SortField) => {
        if (sortField === field) {
            setSortDirection(prev => prev === 'asc' ? 'desc' : 'asc');
        } else {
            setSortField(field);
            setSortDirection('asc');
        }
    };

    const SortIcon = ({ field }: { field: SortField }) => {
        if (sortField !== field) return <ChevronDown className="w-3 h-3 text-gray-300" />;
        return sortDirection === 'asc'
            ? <ChevronUp className="w-3 h-3 text-primary-600" />
            : <ChevronDown className="w-3 h-3 text-primary-600" />;
    };

    const handleSelect = (party: typeof filteredParties[0]) => {
        setSelectedId(party.id);
        setSelectedType(party.type);
    };

    const handleConfirm = () => {
        if (selectedId) {
            const party = allParties.find(p => p.id === selectedId);
            if (party) {
                onSelect(party.type, party.id, party.name);
                onClose();
                // Reset state
                setSearchQuery('');
                setSelectedId(null);
            }
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 bg-black/50 z-[60] flex items-center justify-center p-4">
            <div className="bg-white rounded-xl shadow-2xl w-full max-w-3xl max-h-[85vh] flex flex-col animate-in fade-in zoom-in duration-200">
                {/* Header */}
                <div className="px-6 py-4 border-b border-gray-100 flex justify-between items-center bg-gray-50 rounded-t-xl">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-lg bg-primary-100 flex items-center justify-center">
                            <Users className="w-5 h-5 text-primary-600" />
                        </div>
                        <div>
                            <h3 className="font-bold text-lg text-slate-800">Select Client or Lead</h3>
                            <p className="text-xs text-gray-500">Search and select from your contact list</p>
                        </div>
                    </div>
                    <button
                        onClick={onClose}
                        className="p-2 hover:bg-gray-100 rounded-lg text-gray-400 hover:text-gray-600 transition-colors"
                    >
                        <X className="w-5 h-5" />
                    </button>
                </div>

                {/* Search and Filters */}
                <div className="px-6 py-4 border-b border-gray-100 space-y-3">
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
                        <input
                            type="text"
                            placeholder="Search by name, email, company or client number..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            autoFocus
                            className="w-full pl-11 pr-4 py-3 rounded-lg border border-gray-200 bg-white text-sm focus:ring-2 focus:ring-primary-500 outline-none"
                        />
                        {searchQuery && (
                            <button
                                onClick={() => setSearchQuery('')}
                                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                            >
                                <X className="w-4 h-4" />
                            </button>
                        )}
                    </div>

                    <div className="flex gap-2">
                        <button
                            onClick={() => setFilterType('all')}
                            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${filterType === 'all'
                                ? 'bg-slate-800 text-white'
                                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                                }`}
                        >
                            All ({allParties.length})
                        </button>
                        <button
                            onClick={() => setFilterType('client')}
                            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${filterType === 'client'
                                ? 'bg-blue-600 text-white'
                                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                                }`}
                        >
                            Clients ({clients.length})
                        </button>
                        <button
                            onClick={() => setFilterType('lead')}
                            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${filterType === 'lead'
                                ? 'bg-amber-600 text-white'
                                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                                }`}
                        >
                            Leads ({leads.length})
                        </button>
                    </div>
                </div>

                {/* Table */}
                <div className="flex-1 overflow-auto">
                    <table className="w-full text-left">
                        <thead className="bg-gray-50 sticky top-0 z-10">
                            <tr>
                                <th className="w-12 px-4 py-3"></th>
                                <th
                                    className="px-4 py-3 text-xs font-bold text-gray-500 uppercase cursor-pointer hover:text-slate-800"
                                    onClick={() => handleSort('name')}
                                >
                                    <div className="flex items-center gap-1">
                                        Name <SortIcon field="name" />
                                    </div>
                                </th>
                                <th
                                    className="px-4 py-3 text-xs font-bold text-gray-500 uppercase cursor-pointer hover:text-slate-800"
                                    onClick={() => handleSort('email')}
                                >
                                    <div className="flex items-center gap-1">
                                        Email <SortIcon field="email" />
                                    </div>
                                </th>
                                <th
                                    className="px-4 py-3 text-xs font-bold text-gray-500 uppercase cursor-pointer hover:text-slate-800"
                                    onClick={() => handleSort('company')}
                                >
                                    <div className="flex items-center gap-1">
                                        Company <SortIcon field="company" />
                                    </div>
                                </th>
                                <th
                                    className="px-4 py-3 text-xs font-bold text-gray-500 uppercase cursor-pointer hover:text-slate-800"
                                    onClick={() => handleSort('type')}
                                >
                                    <div className="flex items-center gap-1">
                                        Type <SortIcon field="type" />
                                    </div>
                                </th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100">
                            {filteredParties.length === 0 ? (
                                <tr>
                                    <td colSpan={5} className="px-4 py-12 text-center text-gray-400">
                                        <Users className="w-12 h-12 mx-auto opacity-20 mb-2" />
                                        <p className="font-medium">No contacts found</p>
                                        <p className="text-sm">Try adjusting your search or filters</p>
                                    </td>
                                </tr>
                            ) : (
                                filteredParties.map((party) => (
                                    <tr
                                        key={`${party.type}-${party.id}`}
                                        className={`cursor-pointer transition-colors ${selectedId === party.id
                                            ? 'bg-primary-50 border-l-4 border-l-primary-500'
                                            : 'hover:bg-gray-50'
                                            }`}
                                        onClick={() => handleSelect(party)}
                                        onDoubleClick={() => {
                                            handleSelect(party);
                                            onSelect(party.type, party.id, party.name);
                                            onClose();
                                        }}
                                    >
                                        <td className="px-4 py-3">
                                            <div className={`w-6 h-6 rounded-full border-2 flex items-center justify-center transition-colors ${selectedId === party.id
                                                ? 'bg-primary-600 border-primary-600'
                                                : 'border-gray-300 bg-white'
                                                }`}>
                                                {selectedId === party.id && <Check className="w-3 h-3 text-white" />}
                                            </div>
                                        </td>
                                        <td className="px-4 py-3">
                                            <div className="flex items-center gap-3">
                                                <div className={`w-9 h-9 rounded-full flex items-center justify-center text-xs font-bold uppercase ${party.type === 'client'
                                                    ? 'bg-blue-100 text-blue-700'
                                                    : 'bg-amber-100 text-amber-700'
                                                    }`}>
                                                    {party.name.substring(0, 2)}
                                                </div>
                                                <div>
                                                    <p className="font-semibold text-slate-800 text-sm">{party.name}</p>
                                                    {party.clientNumber && (
                                                        <p className="text-xs text-gray-400 font-mono">{party.clientNumber}</p>
                                                    )}
                                                </div>
                                            </div>
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-600">
                                            {party.email || <span className="text-gray-300">—</span>}
                                        </td>
                                        <td className="px-4 py-3 text-sm text-gray-600">
                                            {party.company || <span className="text-gray-300">—</span>}
                                        </td>
                                        <td className="px-4 py-3">
                                            <span className={`px-2.5 py-1 rounded-full text-xs font-bold ${party.type === 'client'
                                                ? 'bg-blue-100 text-blue-700'
                                                : 'bg-amber-100 text-amber-700'
                                                }`}>
                                                {party.type === 'client' ? 'Client' : 'Lead'}
                                            </span>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>

                {/* Footer */}
                <div className="px-6 py-4 border-t border-gray-100 flex justify-between items-center bg-gray-50 rounded-b-xl">
                    <p className="text-sm text-gray-500">
                        {filteredParties.length} contact{filteredParties.length !== 1 ? 's' : ''} found
                        {selectedId && (
                            <span className="ml-2 text-primary-600 font-medium">
                                • 1 selected
                            </span>
                        )}
                    </p>
                    <div className="flex gap-3">
                        <button
                            onClick={onClose}
                            className="px-4 py-2.5 text-sm font-bold text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
                        >
                            Cancel
                        </button>
                        <button
                            onClick={handleConfirm}
                            disabled={!selectedId}
                            className={`px-6 py-2.5 text-sm font-bold rounded-lg transition-colors ${selectedId
                                ? 'bg-slate-800 text-white hover:bg-slate-900'
                                : 'bg-gray-200 text-gray-400 cursor-not-allowed'
                                }`}
                        >
                            Select
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ClientSelectorModal;
