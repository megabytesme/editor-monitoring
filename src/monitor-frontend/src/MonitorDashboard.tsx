import React, { useState, useEffect, useRef, useCallback } from 'react';
import './MonitorDashboard.css';

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
}

const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    return `${date.toLocaleDateString()} ${date.toLocaleTimeString()}`;
};

const MonitorDashboard: React.FC = () => {
    const [endpoints, setEndpoints] = useState<EndpointConfig[]>([]);
    const [results, setResults] = useState<StatusResult[]>([]);
    const [newEndpoint, setNewEndpoint] = useState<EndpointConfig>({ id: 0, friendlyName: '', url: '' });
    const [settings, setSettings] = useState<SettingsConfig>({ checkInterval: 30 });
    const intervalIdRef = useRef<number | null>(null);

    const fetchEndpoints = async () => {
        try {
            const response = await fetch('http://localhost:5000/api/config/endpoints', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data: EndpointConfig[] = await response.json();
            setEndpoints(data); // Store the endpoints without additional metrics here
        } catch (error) {
            console.error('Error fetching endpoints:', error);
        }
    };

    const fetchStatusResults = async () => {
        try {
            const response = await fetch('http://localhost:5000/api/status/results', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data: StatusResult[] = await response.json();
            setResults(data);
        } catch (error) {
            console.error('Error fetching status results:', error);
        }
    };

    const fetchSettings = async () => {
        try {
            const response = await fetch('http://localhost:5000/api/config/settings', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data: SettingsConfig = await response.json();
            setSettings(data);
        } catch (error) {
            console.error('Error fetching settings:', error);
        }
    };

    const handleRemoveEndpoint = async (id: number) => {
        try {
            const response = await fetch(`http://localhost:5000/api/config/endpoints/${id}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            fetchEndpoints();
            fetchStatusResults();
        } catch (error) {
            console.error('Error removing endpoint:', error);
        }
    };

    const handleAddEndpoint = async () => {
        try {
            const response = await fetch('http://localhost:5000/api/config/endpoints', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(newEndpoint),
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            setEndpoints([...endpoints, data]);
            setNewEndpoint({ id: 0, friendlyName: '', url: '' });
            fetchStatusResults();
        } catch (error) {
            console.error('Error adding endpoint:', error);
        }
    };

    const handleCheckIntervalChange = async (newInterval: number) => {
        try {
            const response = await fetch('http://localhost:5000/api/config/settings', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ id: 1, checkInterval: newInterval }),
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

    const handleTestEndpoint = async (endpoint: string) => {
        try {
            const response = await fetch('http://localhost:5000/api/EndpointTester/test-endpoint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ endpoint }),
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            fetchStatusResults();
        } catch (error) {
            console.error('Error testing endpoint:', error);
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
                                    const relevantResults = results.filter(r => r.endpoint === endpoint.url)
                                                                .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
                                                                .slice(0, 3);
                                    const avgDuration = relevantResults.length ? 
                                                        (relevantResults.reduce((acc, curr) => acc + curr.duration, 0) / relevantResults.length).toFixed(2) 
                                                        : 'N/A';
                                    return `${avgDuration} ms`;
                                })()}
                            </p>
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
            </div>
        </div>
    );
};

export default MonitorDashboard;
