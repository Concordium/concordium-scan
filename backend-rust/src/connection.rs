use async_graphql::connection;

use crate::graphql_api::{ApiError, ApiResult};

/// Construct a GraphQL Cursor Connection response from slice.
/// The cursor is the index in the slice.
pub fn connection_from_slice<T: AsRef<[A]>, A: async_graphql::OutputType + Clone>(
    collection: T,
    first: Option<usize>,
    after: Option<String>,
    last: Option<usize>,
    before: Option<String>,
) -> ApiResult<connection::Connection<String, A>> {
    let collection = collection.as_ref();

    if first.is_some() && last.is_some() {
        return Err(ApiError::QueryConnectionFirstLast);
    }
    let mut start: usize = if let Some(after) = after {
        after.parse::<usize>()? + 1
    } else {
        0
    };
    let mut end: usize = if let Some(before) = before {
        before.parse::<usize>()?
    } else {
        collection.len()
    };
    if let Some(first) = first {
        end = (start + first).min(end);
    }
    if let Some(last) = last {
        start = if last > end - start {
            end
        } else {
            end - last
        };
    }
    let range = start..end;
    let slice = &collection[range.clone()];
    let mut connection = connection::Connection::new(start > 0, end < collection.len());
    for (i, item) in range.zip(slice.iter().cloned()) {
        connection.edges.push(connection::Edge::new(i.to_string(), item))
    }
    Ok(connection)
}

/// Upper and lower limits for the Cursor in a GraphQL Cursor Connection.
pub trait ConnectionCursor {
    const MIN: Self;
    const MAX: Self;
}
impl ConnectionCursor for i64 {
    const MAX: i64 = i64::MAX;
    const MIN: i64 = i64::MIN;
}

/// Prepared query arguments for SQL query, based on arguments from a GraphQL
/// Cursor Connection resolver.
pub struct ConnectionQuery<A> {
    /// The lower to use for the SQL query.
    pub from:  A,
    /// The upper to use for the SQL query.
    pub to:    A,
    /// The limit to use for the SQL query.
    pub limit: i64,
    /// If the `last` elements are requested instead of the `first` elements
    /// (indicated by the `last` key being set when creating a new
    /// `ConnectionQuery`), the edges/nodes should be ordered in reverse
    /// (DESC) order before applying the range. This allows the range from
    /// `from` to `to` to be applied starting from the last element.
    pub desc:  bool,
}
impl<A> ConnectionQuery<A> {
    /// Validate and prepare GraphQL Cursor Connection arguments to be used for
    /// a querying a collection stored in the database.
    pub fn new<E>(
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        connection_limit: u64,
    ) -> ApiResult<Self>
    where
        A: std::str::FromStr<Err = E> + ConnectionCursor,
        E: Into<ApiError>, {
        if first.is_some() && last.is_some() {
            return Err(ApiError::QueryConnectionFirstLast);
        }

        let from = if let Some(a) = after {
            a.parse::<A>().map_err(|e| e.into())?
        } else {
            A::MIN
        };

        let to = if let Some(b) = before {
            b.parse::<A>().map_err(|e| e.into())?
        } else {
            A::MAX
        };

        let limit =
            first.or(last).map_or(connection_limit, |limit| connection_limit.min(limit)) as i64;

        Ok(Self {
            from,
            to,
            limit,
            desc: last.is_some(),
        })
    }
}
