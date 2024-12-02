export interface RequestConfig {
    method: 'GET' | 'POST' | 'PUT' | 'DELETE';
    url: string;
    headers?: Record<string, string>;
    body?: any;
}

export const httpRequest = async <T>(config: RequestConfig): Promise<T> => {
    const { method, url, headers, body } = config;

    console.log("Making request with config:", config);

    try {
        const response = await fetch(url, {
            method,
            headers: {
                'Content-Type': 'application/json',
                ...headers,
            },
            body: body ? JSON.stringify(body) : null,
            credentials: 'include',
        });

        console.log("Received response:", response);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data: T = await response.json();
        console.log("Response data:", data);
        return data;
    } catch (error) {
        console.error('Error fetching data:', error);
        throw error;
    }
};
