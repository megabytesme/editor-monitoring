import React, { useState } from 'react';
import { httpRequest } from './httpService';

interface TestResult {
    message: string;
}

const MonitorDashboard: React.FC = () => {
    const [endpoint, setEndpoint] = useState('');
    const [results, setResults] = useState<TestResult[]>([]);

    const handleTestEndpoint = async () => {
        try {
            const response = await httpRequest<TestResult[]>({
                method: 'POST',
                url: 'http://localhost:5000/api/EndpointTester/test-endpoint',
                body: { endpoint },
            });

            if (Array.isArray(response)) {
                setResults(response);
            } else {
                setResults([response]);
            }
        } catch (error) {
            console.error('Error testing endpoint:', error);
            setResults([]);
        }
    };

    return (
        <div>
            <h1>Monitoring Dashboard</h1>
            <input
                type="text"
                value={endpoint}
                onChange={(e) => setEndpoint(e.target.value)}
                placeholder="Enter endpoint"
            />
            <button onClick={handleTestEndpoint}>Test Endpoint</button>

            <h2>Results</h2>
            <ul>
                {results.map((result, index) => (
                    <li key={index}>{result.message}</li>
                ))}
            </ul>
        </div>
    );
};

export default MonitorDashboard;
