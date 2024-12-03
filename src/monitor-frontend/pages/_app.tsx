import '../styles/index.css';

type AppProps = {
  Component: React.ComponentType;
  pageProps: any;
};

function MyApp({ Component, pageProps }: AppProps) {
  return <Component {...pageProps} />;
}

export default MyApp;
