'use client';

import { useState, useEffect } from 'react';
import { Calendar, Clock, AlertTriangle, CheckCircle, ChevronRight, Plus, Timer } from './Icons';
import { api } from '../services/api';

interface Deadline {
    id: string;
    matterId: string;
    title: string;
    description?: string;
    dueDate: string;
    status: string;
    priority: string;
    deadlineType: string;
}

interface UpcomingDeadlinesWidgetProps {
    days?: number;
    matterId?: string;
    showCreateButton?: boolean;
    onCreateClick?: () => void;
    onDeadlineClick?: (deadline: Deadline) => void;
}

export default function UpcomingDeadlinesWidget({
    days = 7,
    matterId,
    showCreateButton = false,
    onCreateClick,
    onDeadlineClick
}: UpcomingDeadlinesWidgetProps) {
    const [data, setData] = useState<{
        overdue: Deadline[];
        dueToday: Deadline[];
        upcoming: Deadline[];
        totalCount: number;
    } | null>(null);
    const [loading, setLoading] = useState(true);
    const [completing, setCompleting] = useState<string | null>(null);

    useEffect(() => {
        loadDeadlines();
    }, [days, matterId]);

    const loadDeadlines = async () => {
        setLoading(true);
        try {
            const result = await api.deadlines.upcoming(days);

            // Filter by matterId if provided
            if (matterId) {
                result.overdue = result.overdue.filter((d: Deadline) => d.matterId === matterId);
                result.dueToday = result.dueToday.filter((d: Deadline) => d.matterId === matterId);
                result.upcoming = result.upcoming.filter((d: Deadline) => d.matterId === matterId);
                result.totalCount = result.overdue.length + result.dueToday.length + result.upcoming.length;
            }

            setData(result);
        } catch (error) {
            console.error('Failed to load deadlines:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleComplete = async (id: string, e: React.MouseEvent) => {
        e.stopPropagation();
        setCompleting(id);
        try {
            await api.deadlines.complete(id);
            await loadDeadlines();
        } catch (error) {
            console.error('Failed to complete deadline:', error);
        } finally {
            setCompleting(null);
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        const today = new Date();
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);

        if (date.toDateString() === today.toDateString()) {
            return 'Today';
        } else if (date.toDateString() === tomorrow.toDateString()) {
            return 'Tomorrow';
        }

        return date.toLocaleDateString('en-US', {
            weekday: 'short',
            month: 'short',
            day: 'numeric'
        });
    };

    const getDaysUntil = (dateString: string) => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const due = new Date(dateString);
        due.setHours(0, 0, 0, 0);
        return Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    };

    const getPriorityColor = (priority: string) => {
        switch (priority) {
            case 'High': return 'text-red-600';
            case 'Medium': return 'text-amber-600';
            default: return 'text-slate-500';
        }
    };

    const renderDeadline = (deadline: Deadline, isOverdue: boolean = false) => (
        <div
            key={deadline.id}
            onClick={() => onDeadlineClick?.(deadline)}
            className={`flex items-center gap-3 p-3 rounded-lg transition cursor-pointer ${isOverdue ? 'bg-red-50 hover:bg-red-100' : 'bg-slate-50 hover:bg-slate-100'
                }`}
        >
            <button
                onClick={(e) => handleComplete(deadline.id, e)}
                disabled={completing === deadline.id}
                className={`w-5 h-5 rounded-full border-2 flex items-center justify-center flex-shrink-0 transition ${isOverdue
                        ? 'border-red-400 hover:bg-red-200'
                        : 'border-slate-300 hover:bg-slate-200'
                    }`}
            >
                {completing === deadline.id && (
                    <div className="w-3 h-3 border border-slate-400 border-t-transparent rounded-full animate-spin" />
                )}
            </button>

            <div className="flex-1 min-w-0">
                <p className={`font-medium truncate ${isOverdue ? 'text-red-800' : 'text-slate-800'}`}>
                    {deadline.title}
                </p>
                <div className="flex items-center gap-2 text-xs">
                    <span className={isOverdue ? 'text-red-600 font-medium' : 'text-slate-500'}>
                        {formatDate(deadline.dueDate)}
                    </span>
                    <span className={`px-1.5 py-0.5 rounded ${deadline.deadlineType === 'Filing' ? 'bg-blue-100 text-blue-700' :
                            deadline.deadlineType === 'Hearing' ? 'bg-purple-100 text-purple-700' :
                                'bg-slate-100 text-slate-600'
                        }`}>
                        {deadline.deadlineType}
                    </span>
                </div>
            </div>

            <div className="flex items-center gap-2">
                {deadline.priority === 'High' && (
                    <AlertTriangle className="w-4 h-4 text-red-500" />
                )}
                <ChevronRight className="w-4 h-4 text-slate-400" />
            </div>
        </div>
    );

    if (loading) {
        return (
            <div className="bg-white rounded-xl border border-slate-200 p-6">
                <div className="flex items-center justify-center h-32">
                    <div className="w-6 h-6 border-2 border-blue-200 border-t-blue-600 rounded-full animate-spin" />
                </div>
            </div>
        );
    }

    if (!data) return null;

    const hasDeadlines = data.totalCount > 0;

    return (
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
            {/* Header */}
            <div className="px-4 py-3 border-b bg-gradient-to-r from-amber-50 to-orange-50 flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <Timer className="w-5 h-5 text-amber-600" />
                    <h3 className="font-semibold text-slate-800">Upcoming Deadlines</h3>
                    {data.totalCount > 0 && (
                        <span className="px-2 py-0.5 bg-amber-100 text-amber-700 rounded-full text-xs font-medium">
                            {data.totalCount}
                        </span>
                    )}
                </div>
                {showCreateButton && (
                    <button
                        onClick={onCreateClick}
                        className="p-1.5 hover:bg-white/50 rounded transition"
                    >
                        <Plus className="w-4 h-4" />
                    </button>
                )}
            </div>

            <div className="p-4">
                {!hasDeadlines ? (
                    <div className="text-center py-8 text-slate-500">
                        <Calendar className="w-10 h-10 mx-auto mb-2 opacity-30" />
                        <p className="text-sm">No upcoming deadlines</p>
                        <p className="text-xs">You're all caught up!</p>
                    </div>
                ) : (
                    <div className="space-y-4">
                        {/* Overdue */}
                        {data.overdue.length > 0 && (
                            <div>
                                <div className="flex items-center gap-2 mb-2">
                                    <AlertTriangle className="w-4 h-4 text-red-500" />
                                    <span className="text-sm font-medium text-red-600">
                                        Overdue ({data.overdue.length})
                                    </span>
                                </div>
                                <div className="space-y-2">
                                    {data.overdue.map(d => renderDeadline(d, true))}
                                </div>
                            </div>
                        )}

                        {/* Due Today */}
                        {data.dueToday.length > 0 && (
                            <div>
                                <div className="flex items-center gap-2 mb-2">
                                    <Clock className="w-4 h-4 text-amber-500" />
                                    <span className="text-sm font-medium text-amber-600">
                                        Due Today ({data.dueToday.length})
                                    </span>
                                </div>
                                <div className="space-y-2">
                                    {data.dueToday.map(d => renderDeadline(d))}
                                </div>
                            </div>
                        )}

                        {/* Upcoming */}
                        {data.upcoming.length > 0 && (
                            <div>
                                <div className="flex items-center gap-2 mb-2">
                                    <Calendar className="w-4 h-4 text-blue-500" />
                                    <span className="text-sm font-medium text-slate-600">
                                        Coming Up ({data.upcoming.length})
                                    </span>
                                </div>
                                <div className="space-y-2">
                                    {data.upcoming.slice(0, 5).map(d => renderDeadline(d))}
                                    {data.upcoming.length > 5 && (
                                        <p className="text-center text-sm text-slate-500 pt-2">
                                            +{data.upcoming.length - 5} more
                                        </p>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}
