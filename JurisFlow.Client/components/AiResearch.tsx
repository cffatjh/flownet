'use client';

import { useState, useEffect } from 'react';
import { Search, BookOpen, Sparkles, Clock, ChevronRight, Send, Link, FileText } from './Icons';
import { api } from '../services/api';

interface ResearchSession {
    id: string;
    title: string;
    query: string;
    response?: string;
    citations?: string[];
    keyPoints?: string[];
    relatedCases?: string[];
    status: string;
    jurisdiction?: string;
    practiceArea?: string;
    processingTimeMs?: number;
    createdAt: string;
}

interface AiResearchProps {
    matterId?: string;
    onAttachToMatter?: (sessionId: string) => void;
}

export default function AiResearch({ matterId, onAttachToMatter }: AiResearchProps) {
    const [query, setQuery] = useState('');
    const [jurisdiction, setJurisdiction] = useState('');
    const [practiceArea, setPracticeArea] = useState('');
    const [searching, setSearching] = useState(false);
    const [result, setResult] = useState<ResearchSession | null>(null);
    const [history, setHistory] = useState<ResearchSession[]>([]);
    const [showHistory, setShowHistory] = useState(false);

    const jurisdictions = [
        'Federal', 'California', 'New York', 'Texas', 'Florida',
        'Illinois', 'Pennsylvania', 'Ohio', 'Georgia', 'Michigan'
    ];

    const practiceAreas = [
        'Personal Injury', 'Family Law', 'Criminal Defense', 'Immigration',
        'Business Law', 'Real Estate', 'Estate Planning', 'Bankruptcy',
        'Employment Law', 'Intellectual Property'
    ];

    useEffect(() => {
        loadHistory();
    }, [matterId]);

    const loadHistory = async () => {
        try {
            const data = await api.ai.research.list({ matterId, limit: 10 });
            setHistory(data);
        } catch (error) {
            console.error('Failed to load history:', error);
        }
    };

    const handleSearch = async () => {
        if (!query.trim()) return;

        setSearching(true);
        setResult(null);

        try {
            const response = await api.ai.research.start({
                query,
                matterId,
                jurisdiction: jurisdiction || undefined,
                practiceArea: practiceArea || undefined
            });

            // Parse JSON fields
            const session: ResearchSession = {
                ...response,
                citations: response.citationsJson ? JSON.parse(response.citationsJson) : [],
                keyPoints: response.keyPointsJson ? JSON.parse(response.keyPointsJson) : [],
                relatedCases: response.relatedCasesJson ? JSON.parse(response.relatedCasesJson) : []
            };

            setResult(session);
            await loadHistory();
        } catch (error) {
            console.error('Failed to search:', error);
        } finally {
            setSearching(false);
        }
    };

    const loadSession = async (id: string) => {
        try {
            const response = await api.ai.research.get(id);
            const session: ResearchSession = {
                ...response,
                citations: response.citationsJson ? JSON.parse(response.citationsJson) : [],
                keyPoints: response.keyPointsJson ? JSON.parse(response.keyPointsJson) : [],
                relatedCases: response.relatedCasesJson ? JSON.parse(response.relatedCasesJson) : []
            };
            setResult(session);
            setQuery(session.query);
            setShowHistory(false);
        } catch (error) {
            console.error('Failed to load session:', error);
        }
    };

    return (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
            {/* Header */}
            <div className="px-6 py-4 border-b bg-gradient-to-r from-violet-50 to-purple-50">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-full bg-gradient-to-br from-violet-500 to-purple-600 flex items-center justify-center">
                            <Sparkles className="w-5 h-5 text-white" />
                        </div>
                        <div>
                            <h2 className="text-lg font-semibold text-slate-800">AI Legal Research</h2>
                            <p className="text-sm text-slate-500">Powered by Gemini</p>
                        </div>
                    </div>
                    {history.length > 0 && (
                        <button
                            onClick={() => setShowHistory(!showHistory)}
                            className="text-sm text-violet-600 hover:text-violet-700 flex items-center gap-1"
                        >
                            <Clock className="w-4 h-4" />
                            History
                        </button>
                    )}
                </div>
            </div>

            {/* History Dropdown */}
            {showHistory && (
                <div className="border-b bg-slate-50 max-h-48 overflow-auto">
                    {history.map(session => (
                        <button
                            key={session.id}
                            onClick={() => loadSession(session.id)}
                            className="w-full px-6 py-3 text-left hover:bg-white transition flex items-center justify-between"
                        >
                            <div>
                                <p className="font-medium text-sm text-slate-800 truncate">{session.title}</p>
                                <p className="text-xs text-slate-500">
                                    {new Date(session.createdAt).toLocaleDateString()}
                                </p>
                            </div>
                            <ChevronRight className="w-4 h-4 text-slate-400" />
                        </button>
                    ))}
                </div>
            )}

            {/* Search Form */}
            <div className="p-6">
                <div className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-slate-700 mb-1">
                            Legal Question or Research Topic
                        </label>
                        <textarea
                            value={query}
                            onChange={(e) => setQuery(e.target.value)}
                            placeholder="e.g., What are the elements required to prove negligence in a slip and fall case?"
                            rows={3}
                            className="w-full px-4 py-3 border border-slate-200 rounded-lg focus:ring-2 focus:ring-violet-500"
                        />
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">
                                Jurisdiction
                            </label>
                            <select
                                value={jurisdiction}
                                onChange={(e) => setJurisdiction(e.target.value)}
                                className="w-full px-4 py-2 border border-slate-200 rounded-lg"
                            >
                                <option value="">Any Jurisdiction</option>
                                {jurisdictions.map(j => (
                                    <option key={j} value={j}>{j}</option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-slate-700 mb-1">
                                Practice Area
                            </label>
                            <select
                                value={practiceArea}
                                onChange={(e) => setPracticeArea(e.target.value)}
                                className="w-full px-4 py-2 border border-slate-200 rounded-lg"
                            >
                                <option value="">Any Practice Area</option>
                                {practiceAreas.map(p => (
                                    <option key={p} value={p}>{p}</option>
                                ))}
                            </select>
                        </div>
                    </div>

                    <button
                        onClick={handleSearch}
                        disabled={searching || !query.trim()}
                        className="w-full py-3 bg-gradient-to-r from-violet-600 to-purple-600 text-white rounded-lg font-medium hover:from-violet-700 hover:to-purple-700 disabled:opacity-50 transition flex items-center justify-center gap-2"
                    >
                        {searching ? (
                            <>
                                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                Researching...
                            </>
                        ) : (
                            <>
                                <Search className="w-5 h-5" />
                                Research
                            </>
                        )}
                    </button>
                </div>
            </div>

            {/* Results */}
            {result && (
                <div className="border-t">
                    {/* Response */}
                    <div className="p-6 bg-slate-50">
                        <div className="flex items-center justify-between mb-3">
                            <h3 className="font-semibold text-slate-800 flex items-center gap-2">
                                <BookOpen className="w-5 h-5 text-violet-600" />
                                Research Summary
                            </h3>
                            {result.processingTimeMs && (
                                <span className="text-xs text-slate-500">
                                    {(result.processingTimeMs / 1000).toFixed(1)}s
                                </span>
                            )}
                        </div>
                        <div className="prose prose-sm max-w-none text-slate-700">
                            <div dangerouslySetInnerHTML={{
                                __html: result.response?.replace(/\n/g, '<br>') || ''
                            }} />
                        </div>
                    </div>

                    {/* Key Points */}
                    {result.keyPoints && result.keyPoints.length > 0 && (
                        <div className="px-6 py-4 border-t">
                            <h4 className="font-medium text-slate-700 mb-2">Key Points</h4>
                            <ul className="space-y-2">
                                {result.keyPoints.map((point, i) => (
                                    <li key={i} className="flex items-start gap-2 text-sm text-slate-600">
                                        <span className="w-5 h-5 rounded-full bg-violet-100 text-violet-600 flex items-center justify-center text-xs flex-shrink-0">
                                            {i + 1}
                                        </span>
                                        {point}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {/* Citations */}
                    {result.citations && result.citations.length > 0 && (
                        <div className="px-6 py-4 border-t bg-amber-50/50">
                            <h4 className="font-medium text-slate-700 mb-2 flex items-center gap-2">
                                <FileText className="w-4 h-4" />
                                Citations
                            </h4>
                            <ul className="space-y-1">
                                {result.citations.map((citation, i) => (
                                    <li key={i} className="text-sm text-slate-600">
                                        • {citation}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {/* Related Cases */}
                    {result.relatedCases && result.relatedCases.length > 0 && (
                        <div className="px-6 py-4 border-t">
                            <h4 className="font-medium text-slate-700 mb-2">Related Cases</h4>
                            <div className="space-y-2">
                                {result.relatedCases.map((case_, i) => (
                                    <div key={i} className="flex items-center gap-2 text-sm text-slate-600">
                                        <Link className="w-4 h-4 text-blue-500" />
                                        {case_}
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}

                    {/* Actions */}
                    {matterId && onAttachToMatter && (
                        <div className="px-6 py-4 border-t bg-slate-50">
                            <button
                                onClick={() => onAttachToMatter(result.id)}
                                className="w-full py-2 border-2 border-dashed border-violet-300 text-violet-600 rounded-lg hover:bg-violet-50 transition"
                            >
                                Save to Matter
                            </button>
                        </div>
                    )}

                    {/* Disclaimer */}
                    <div className="px-6 py-3 border-t bg-amber-50 text-xs text-amber-700">
                        ⚠️ AI-generated research should be verified by legal counsel before reliance.
                    </div>
                </div>
            )}
        </div>
    );
}
