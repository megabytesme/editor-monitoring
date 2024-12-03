import React, { useState, useEffect, useRef, useCallback } from 'react';
import './MonitorDashboard.css';
import { httpRequest } from './httpService';

interface EndpointConfig {
    id: number;
    friendlyName: string;
    url: string;
    lastResponseDuration?: number;
}

interface StatusResult {
    id: number;
    endpoint: string;
    response: string;
    status: number;
    timestamp: string;
    duration: number;
}

interface SettingsConfig {
    checkInterval: number;
    avgResponseTimeWindow?: number;
}

const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
};

const MonitorDashboard: React.FC = () => {
    const [endpoints, setEndpoints] = useState<EndpointConfig[]>([]);
    const [results, setResults] = useState<StatusResult[]>([]);
    const [newEndpoint, setNewEndpoint] = useState<EndpointConfig>({ id: 0, friendlyName: '', url: '' });
    const [settings, setSettings] = useState<SettingsConfig>({ checkInterval: 30, avgResponseTimeWindow: 1 });
    const intervalIdRef = useRef<number | null>(null);

    const fetchEndpoints = async () => {
        try {
            const data = await httpRequest<EndpointConfig[]>({
                method: 'GET',
                url: 'http://localhost:5000/api/config/endpoints',
            });
            setEndpoints(data);
        } catch (error) {
            console.error('Error fetching endpoints:', error);
        }
    };
    
    const fetchStatusResults = async () => {
        try {
            const data = await httpRequest<StatusResult[]>({
                method: 'GET',
                url: 'http://localhost:5000/api/status/results',
            });
            setResults(data);
        } catch (error) {
            console.error('Error fetching status results:', error);
        }
    };
    
    const fetchSettings = async () => {
        try {
            const data = await httpRequest<SettingsConfig>({
                method: 'GET',
                url: 'http://localhost:5000/api/config/settings',
            });
            setSettings(data);
        } catch (error) {
            console.error('Error fetching settings:', error);
        }
    };
    
    const handleRemoveEndpoint = async (id: number) => {
        try {
            await httpRequest<void>({
                method: 'DELETE',
                url: `http://localhost:5000/api/config/endpoints/${id}`,
            });
            fetchEndpoints();
            fetchStatusResults();
        } catch (error) {
            console.error('Error removing endpoint:', error);
        }
    };
    
    const handleAddEndpoint = async () => {
        try {
            const data = await httpRequest<EndpointConfig>({
                method: 'POST',
                url: 'http://localhost:5000/api/config/endpoints',
                body: newEndpoint,
            });
            setEndpoints([...endpoints, data]);
            setNewEndpoint({ id: 0, friendlyName: '', url: '' });
            fetchStatusResults();
        } catch (error) {
            console.error('Error adding endpoint:', error);
        }
    };
    
    const handleTestEndpoint = async (endpoint: string) => {
        try {
            await httpRequest<void>({
                method: 'POST',
                url: 'http://localhost:5000/api/EndpointTester/test-endpoint',
                body: { endpoint },
            });
            fetchStatusResults();
        } catch (error) {
            console.error('Error testing endpoint:', error);
        }
    };
    
    const handleCheckIntervalChange = async (newInterval: number) => {
        try {
            const response = await fetch('http://localhost:5000/api/config/settings', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ ...settings, checkInterval: newInterval }),
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            setSettings(data);
        } catch (error) {
            console.error('Error updating check interval:', error);
        }
    };
    
    const handleAvgResponseTimeWindowChange = async (newWindow: number) => {
        try {
            const response = await fetch('http://localhost:5000/api/config/settings', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ ...settings, avgResponseTimeWindow: newWindow }),
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            setSettings(data);
        } catch (error) {
            console.error('Error updating avgResponseTimeWindow:', error);
        }
    };  

    const fetchData = useCallback(() => {
        fetchEndpoints();
        fetchStatusResults();
    }, [settings.checkInterval]);

    useEffect(() => {
        fetchSettings().then(fetchData);
        const intervalId = setInterval(fetchData, settings.checkInterval * 1000) as unknown as number;
        intervalIdRef.current = intervalId;

        return () => {
            if (intervalIdRef.current) {
                clearInterval(intervalIdRef.current);
            }
        };
    }, [fetchData]);

    return (
        <div className="monitor-dashboard">
            <h1>Monitoring Dashboard</h1>

            <div className="grid-container">
                {endpoints.map((endpoint) => {
                    const result = results.find(result => result.endpoint === endpoint.url);
                    return (
                        <div key={endpoint.id} className="card endpoint-box">
                            <h3>{endpoint.friendlyName}</h3>
                            <p>URL: {endpoint.url}</p>
                            <p>Status: {result ? result.status : 'Unknown'}</p>
                            <p>Response: {result ? result.response : 'Unknown'}</p>
                            <p>Last Response Duration: {results.filter(r => r.endpoint === endpoint.url).sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())[0]?.duration.toFixed(2) || 'N/A'} ms</p>
                            <p>Average Response Time: {
                                (() => {
                                    const windowSize = settings.avgResponseTimeWindow || 1;
                                    const relevantResults = results.filter(r => r.endpoint === endpoint.url)
                                        .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
                                        .slice(0, windowSize);
                                    const avgDuration = relevantResults.length ? 
                                        (relevantResults.reduce((acc, curr) => acc + curr.duration, 0) / relevantResults.length).toFixed(2) 
                                        : 'N/A';
                                    return `${avgDuration} ms`;
                                })()
                            }</p>
                            <p>Last Checked: {result ? formatTimestamp(result.timestamp) : 'Never'}</p>
                            <button onClick={() => handleTestEndpoint(endpoint.url)}>Test Endpoint</button>
                            <button onClick={() => handleRemoveEndpoint(endpoint.id)}>Remove</button>
                        </div>
                    );
                })}
            </div>

            <div className="card add-endpoint-card">
                <h2>Add Endpoint</h2>
                <input
                    type="text"
                    placeholder="Friendly Name"
                    value={newEndpoint.friendlyName}
                    onChange={(e) => setNewEndpoint({ ...newEndpoint, friendlyName: e.target.value })}
                />
                <input
                    type="text"
                    placeholder="URL"
                    value={newEndpoint.url}
                    onChange={(e) => setNewEndpoint({ ...newEndpoint, url: e.target.value })}
                />
                <button onClick={handleAddEndpoint}>Add</button>
            </div>

            <div className="card settings-card">
                <h2>Settings</h2>
                <div>
                    <label>Check Interval (seconds): </label>
                    <input
                        type="number"
                        value={settings.checkInterval}
                        onChange={(e) => handleCheckIntervalChange(Number(e.target.value))}
                    />
                </div>
                <div>
                    <label>Average Response Time Window (requests): </label>
                    <input
                        type="number"
                        value={settings.avgResponseTimeWindow || 1}
                        onChange={(e) => handleAvgResponseTimeWindowChange(Number(e.target.value))}
                    />
                </div>
            </div>
        </div>
    );
};

export default MonitorDashboard;
