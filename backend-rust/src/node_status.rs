use std::time::Duration;
use concordium_rust_sdk::v2::Client;

struct NodeCollectorBackend {
    client: Client,
    url: String,
    timeout: Duration
}

impl NodeCollectorBackend
{
    /// Creates a new instance of `GoogleCloud` configured for interacting with
    /// the Google Cloud Messaging API.
    ///
    /// # Arguments
    /// * `client` - A `reqwest::Client` used for making HTTP requests.
    /// * `backoff_policy` - An `ExponentialBackoff` policy to handle retries
    ///   for transient errors.
    /// * `service_account` - An implementation of the `TokenProvider` trait to
    ///   fetch access tokens.
    /// * `project_id` - The project ID associated with your Google Cloud
    ///   project.
    ///
    /// # Returns
    /// Returns an instance of `GoogleCloud`.
    pub fn new(
        client: Client,
        url: String,
        timeout: Duration
    ) -> Self {
        Self {
            client,
            url,
            timeout
        }
    }
}