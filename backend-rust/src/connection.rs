use async_graphql::connection;
use std::cmp::min;

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
    if first.is_some() && last.is_some() {
        return Err(ApiError::QueryConnectionFirstLast);
    }
    let collection = collection.as_ref();
    let length = collection.len();
    if length == 0 {
        return Ok(connection::Connection::new(false, false));
    }
    let after_cursor_index = if let Some(after_cursor) = after {
        let index = after_cursor.parse::<usize>()?;
        min(index + 1, length)
    } else {
        0
    };

    let before_cursor_index = if let Some(before_cursor) = before {
        min(before_cursor.parse::<usize>()?, length)
    } else {
        length
    };

    let (start, end) = if let Some(first_count) = first {
        if first_count == 0 {
            return Ok(connection::Connection::new(false, false));
        }
        (after_cursor_index, min(after_cursor_index + first_count, length))
    } else if let Some(last_count) = last {
        if last_count == 0 {
            return Ok(connection::Connection::new(false, false));
        }
        (before_cursor_index.saturating_sub(last_count), before_cursor_index)
    } else {
        (after_cursor_index, before_cursor_index)
    };
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

#[cfg(test)]
mod tests {
    use super::*;

    #[derive(Clone, Debug, PartialEq, async_graphql::SimpleObject)]
    struct TestNode {
        id:   i32,
        name: String,
    }

    fn setup_data() -> Vec<TestNode> {
        vec![
            TestNode {
                id:   1,
                name: "A".to_string(),
            },
            TestNode {
                id:   2,
                name: "B".to_string(),
            },
            TestNode {
                id:   3,
                name: "C".to_string(),
            },
            TestNode {
                id:   4,
                name: "D".to_string(),
            },
            TestNode {
                id:   5,
                name: "E".to_string(),
            },
        ]
    }

    #[test]
    fn test_full_collection() {
        let data = setup_data();
        let result = connection_from_slice(&data, None, None, None, None).unwrap();
        assert_eq!(result.has_next_page, false);
        assert_eq!(result.has_previous_page, false);
        assert_eq!(result.edges.len(), 5);
    }

    #[test]
    fn test_first_n_elements() {
        let data = setup_data();
        let result = connection_from_slice(&data, Some(3), None, None, None).unwrap();
        assert_eq!(result.edges.len(), 3);
        assert_eq!(result.has_next_page, true);
        assert_eq!(result.has_previous_page, false);
        for i in 0..3 {
            assert_eq!(result.edges[i].node, data[i]);
        }
    }

    #[test]
    fn test_last_n_elements() {
        let data = setup_data();
        let result = connection_from_slice(&data, None, None, Some(2), None).unwrap();
        assert_eq!(result.edges.len(), 2);
        assert_eq!(result.has_next_page, false);
        assert_eq!(result.has_previous_page, true);
        assert_eq!(result.edges[0].node, data[3]);
        assert_eq!(result.edges[1].node, data[4]);
    }

    #[test]
    fn test_after_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, Some(2), Some("2".to_string()), None, None).unwrap();
        assert_eq!(result.edges.len(), 2);
        assert_eq!(result.has_next_page, false);
        assert_eq!(result.has_previous_page, true);
        assert_eq!(result.edges[0].node, data[3]);
        assert_eq!(result.edges[1].node, data[4]);
    }

    #[test]
    fn test_before_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, None, None, Some(2), Some("4".to_string())).unwrap();
        assert_eq!(result.edges.len(), 2);
        assert_eq!(result.has_next_page, true);
        assert_eq!(result.has_previous_page, true);
        assert_eq!(result.edges[0].node, data[2]);
        assert_eq!(result.edges[1].node, data[3]);
    }

    #[test]
    fn test_after_and_before_cursor() {
        let data = setup_data();
        let result = connection_from_slice(
            &data,
            Some(2),
            Some("1".to_string()),
            None,
            Some("4".to_string()),
        )
        .unwrap();
        assert_eq!(result.edges.len(), 2);
        assert_eq!(result.has_next_page, true);
        assert_eq!(result.has_previous_page, true);
        assert_eq!(result.edges[0].node, data[2]);
        assert_eq!(result.edges[1].node, data[3]);
    }

    #[test]
    fn test_big_after_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, Some(2), Some("10".to_string()), None, None).unwrap();
        assert!(result.edges.is_empty());
        assert_eq!(result.has_next_page, false);
        assert_eq!(result.has_previous_page, true);
    }

    #[test]
    fn test_big_after_before_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, Some(3), None, None, Some("10".to_string())).unwrap();
        assert_eq!(result.edges.len(), 3);
        assert_eq!(result.has_next_page, true);
        assert_eq!(result.has_previous_page, false);
        assert_eq!(result.edges[0].node, data[0]);
        assert_eq!(result.edges[1].node, data[1]);
        assert_eq!(result.edges[2].node, data[2]);
    }

    #[test]
    fn test_first_and_last() {
        let data = setup_data();
        let result = connection_from_slice(&data, Some(3), None, Some(1), Some("10".to_string()));
        assert!(result.is_err());
    }

    #[test]
    fn test_empty_collection() {
        let collection: Vec<TestNode> = Vec::new();
        let result = connection_from_slice(collection, Some(1), Some("0".to_string()), None, None).unwrap();
        assert!(result.edges.is_empty());
        assert_eq!(result.has_next_page, false);
        assert_eq!(result.has_previous_page, false);
    }

    #[test]
    fn test_before_is_zero() {
        let data = setup_data();
        let result = connection_from_slice(data, None, Some("4".to_string()), Some(0), None).unwrap();
        assert!(result.edges.is_empty());
        assert_eq!(result.has_next_page, false);
        assert_eq!(result.has_previous_page, false);
    }
}
