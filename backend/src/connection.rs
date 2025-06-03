use async_graphql::connection::{self, CursorType};
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
        let index = usize::decode_cursor(after_cursor.as_str())?;
        min(index + 1, length)
    } else {
        0
    };

    let before_cursor_index = if let Some(before_cursor) = before {
        min(usize::decode_cursor(before_cursor.as_str())?, length)
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
        connection.edges.push(connection::Edge::new(i.encode_cursor(), item))
    }
    Ok(connection)
}

/// Bounds for the Cursor in a GraphQL Cursor Connection, used as the fallback
/// when no explicit range is provided as `after`/`before`.
pub trait ConnectionBounds {
    /// A non-inclusive bound for the start of the collection provided as the
    /// connection.
    const START_BOUND: Self;
    /// A non-inclusive bound for the end of the collection provided as the
    /// connection.
    const END_BOUND: Self;
}
impl ConnectionBounds for i64 {
    const END_BOUND: i64 = i64::MAX;
    const START_BOUND: i64 = i64::MIN;
}

/// GraphQL Connection Cursor representing a collection where the pages are
/// descending order using i64 as the cursor.
pub type DescendingI64 = Reversed<i64>;

impl From<DescendingI64> for i64 {
    fn from(value: DescendingI64) -> Self { value.into_inner() }
}

impl From<i64> for DescendingI64 {
    fn from(value: i64) -> Self { Reversed::new(value) }
}

/// GraphQL Connection Cursor representing a collection where the pages are
/// reversed order.
/// This wrapper flips the start and end bounds of the [`ConnectionBound`] and
/// implements [`Ord`] which is reverse of the inner type.
#[derive(Debug, Clone, PartialEq, Eq)]
#[repr(transparent)]
pub struct Reversed<Cursor> {
    pub cursor: Cursor,
}
impl<C> Reversed<C> {
    pub const fn new(cursor: C) -> Self {
        Self {
            cursor,
        }
    }

    pub fn into_inner(self) -> C { self.cursor }
}

impl<C> ConnectionBounds for Reversed<C>
where
    C: ConnectionBounds,
{
    const END_BOUND: Self = Self::new(C::START_BOUND);
    const START_BOUND: Self = Self::new(C::END_BOUND);
}
impl<C> CursorType for Reversed<C>
where
    C: CursorType,
{
    type Error = C::Error;

    fn decode_cursor(s: &str) -> Result<Self, Self::Error> {
        C::decode_cursor(s).map(Reversed::new)
    }

    fn encode_cursor(&self) -> String { self.cursor.encode_cursor() }
}

impl<C> PartialOrd for Reversed<C>
where
    C: PartialOrd,
{
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> {
        self.cursor.partial_cmp(&other.cursor).map(|ord| ord.reverse())
    }
}

impl<C> Ord for Reversed<C>
where
    C: Ord,
{
    fn cmp(&self, other: &Self) -> std::cmp::Ordering { self.cursor.cmp(&other.cursor).reverse() }
}

/// Construct for combining two connection cursors into one, where the two
/// connections are considered concatenated into one.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord)]
pub(crate) enum ConcatCursor<Fst, Snd> {
    First(Fst),
    Second(Snd),
}
impl<Fst, Snd> ConcatCursor<Fst, Snd> {
    /// Get the cursor for the inner connection considered first.
    pub fn first(&self) -> Option<&Fst> {
        match self {
            ConcatCursor::First(fst) => Some(fst),
            ConcatCursor::Second(_) => None,
        }
    }

    /// Get the cursor for the inner connection considered second.
    pub fn second(&self) -> Option<&Snd> {
        match self {
            ConcatCursor::First(_) => None,
            ConcatCursor::Second(snd) => Some(snd),
        }
    }
}

impl<Fst, Snd> ConnectionBounds for ConcatCursor<Fst, Snd>
where
    Fst: ConnectionBounds,
    Snd: ConnectionBounds,
{
    const END_BOUND: Self = ConcatCursor::Second(Snd::END_BOUND);
    const START_BOUND: Self = ConcatCursor::First(Fst::START_BOUND);
}

impl<Fst, Snd> connection::CursorType for ConcatCursor<Fst, Snd>
where
    Fst: connection::CursorType,
    Snd: connection::CursorType,
{
    type Error = ConcatCursorDecodeError<Fst::Error, Snd::Error>;

    /// Decode the cursor from a string.
    ///
    /// The format of the cursor is `({before}:{after})` where `{before}` is
    /// either `fst` or `snd` to represent either collections, and `{after}` is
    /// the cursor for this collection.
    fn decode_cursor(value: &str) -> Result<Self, Self::Error> {
        // First trim the `(` and `)`.
        let value = &value[1..value.len() - 1];
        // Search for the `:` but ignore any which are deeper nested in parenthesis,
        // therefor we keep track of the level.
        let mut level = 0;
        let mut split = None;
        for (i, c) in value.chars().enumerate() {
            match c {
                '(' => level += 1,
                ')' => level -= 1,
                ':' if level == 0 => {
                    split = Some(i);
                    break;
                }
                _ => {}
            }
        }
        let split = split.ok_or(ConcatCursorDecodeError::NoSemicolon)?;
        let (before, after): (&str, &str) = value.split_at(split);
        // Since split_at, leaves the `:` in the `after` part, we trim the first
        // character of this:
        let after = &after[1..];
        match before {
            "fst" => {
                let cursor =
                    Fst::decode_cursor(after).map_err(ConcatCursorDecodeError::FirstError)?;
                Ok(ConcatCursor::First(cursor))
            }
            "snd" => {
                let cursor =
                    Snd::decode_cursor(after).map_err(ConcatCursorDecodeError::SecondError)?;
                Ok(ConcatCursor::Second(cursor))
            }
            otherwise => Err(ConcatCursorDecodeError::UnexpectedPrefix(otherwise.to_string())),
        }
    }

    fn encode_cursor(&self) -> String {
        match self {
            ConcatCursor::First(fst) => format!("(fst:{})", fst.encode_cursor()),
            ConcatCursor::Second(snd) => format!("(snd:{})", snd.encode_cursor()),
        }
    }
}

#[derive(Debug, thiserror::Error, Clone)]
pub enum ConcatCursorDecodeError<F, S> {
    #[error("Must contain a semicolon")]
    NoSemicolon,
    #[error("Unexpected prefix for cursor: {0}")]
    UnexpectedPrefix(String),
    #[error("First error: {0}")]
    FirstError(F),
    #[error("Second error: {0}")]
    SecondError(S),
}

impl<F, S> From<ConcatCursorDecodeError<F, S>> for ApiError
where
    F: std::fmt::Display,
    S: std::fmt::Display,
{
    fn from(err: ConcatCursorDecodeError<F, S>) -> Self {
        ApiError::InvalidCursorFormat(err.to_string())
    }
}

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct NestedCursor<Outer, Inner> {
    pub outer: Outer,
    pub inner: Inner,
}
impl<Outer, Inner> ConnectionBounds for NestedCursor<Outer, Inner>
where
    Outer: ConnectionBounds,
    Inner: ConnectionBounds,
{
    const END_BOUND: Self = Self {
        outer: Outer::END_BOUND,
        inner: Inner::END_BOUND,
    };
    const START_BOUND: Self = Self {
        outer: Outer::START_BOUND,
        inner: Inner::START_BOUND,
    };
}

impl<Outer, Inner> connection::CursorType for NestedCursor<Outer, Inner>
where
    Outer: connection::CursorType,
    Inner: connection::CursorType,
{
    type Error = NestedCursorDecodeError<Outer::Error, Inner::Error>;

    /// Decode the cursor from a string.
    ///
    /// The format of the cursor is `({outer}:{inner})` where `{outer}` is
    /// the outermost cursor and `{inner}` is the inner cursor.
    fn decode_cursor(value: &str) -> Result<Self, Self::Error> {
        // First trim the `(` and `)`.
        let value = &value[1..value.len() - 1];
        // Search for the `:` but ignore any which are deeper nested in parenthesis,
        // therefor we keep track of the level.
        let mut level = 0;
        let mut split = None;
        for (i, c) in value.chars().enumerate() {
            match c {
                '(' => level += 1,
                ')' => level -= 1,
                ':' if level == 0 => {
                    split = Some(i);
                    break;
                }
                _ => {}
            }
        }
        let split = split.ok_or(NestedCursorDecodeError::NoSemicolon)?;
        let (before, after): (&str, &str) = value.split_at(split);
        // Since split_at, leaves the `:` in the `after` part, we trim the first
        // character of this:
        let after = &after[1..];
        let outer = Outer::decode_cursor(before).map_err(NestedCursorDecodeError::OuterError)?;
        let inner = Inner::decode_cursor(after).map_err(NestedCursorDecodeError::InnerError)?;
        Ok(Self {
            outer,
            inner,
        })
    }

    fn encode_cursor(&self) -> String {
        format!("({}:{})", self.outer.encode_cursor(), self.inner.encode_cursor())
    }
}

#[derive(Debug, thiserror::Error, Clone)]
pub enum NestedCursorDecodeError<O, I> {
    #[error("Must contain a semicolon")]
    NoSemicolon,
    #[error("Decode outer error: {0}")]
    OuterError(O),
    #[error("Decode inner error: {0}")]
    InnerError(I),
}

impl<O, I> From<NestedCursorDecodeError<I, O>> for ApiError
where
    O: std::fmt::Display,
    I: std::fmt::Display,
{
    fn from(err: NestedCursorDecodeError<I, O>) -> Self {
        ApiError::InvalidCursorFormat(err.to_string())
    }
}

impl<Outer, Inner> PartialOrd for NestedCursor<Outer, Inner>
where
    Outer: PartialOrd,
    Inner: PartialOrd,
{
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> {
        let ordering = self.outer.partial_cmp(&other.outer)?;
        if let std::cmp::Ordering::Equal = ordering {
            self.inner.partial_cmp(&other.inner)
        } else {
            Some(ordering)
        }
    }
}

impl<Outer, Inner> Ord for NestedCursor<Outer, Inner>
where
    Outer: Ord,
    Inner: Ord,
{
    fn cmp(&self, other: &Self) -> std::cmp::Ordering {
        let ordering = self.outer.cmp(&other.outer);
        if let std::cmp::Ordering::Equal = ordering {
            self.inner.cmp(&other.inner)
        } else {
            ordering
        }
    }
}

/// Cursor representing the empty collection.
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord)]
pub struct UnitCursor;

impl ConnectionBounds for UnitCursor {
    const END_BOUND: Self = Self;
    const START_BOUND: Self = Self;
}

impl connection::CursorType for UnitCursor {
    type Error = UnitCursorDecodeError;

    fn decode_cursor(value: &str) -> Result<Self, Self::Error> {
        if value == "unit" {
            Ok(Self)
        } else {
            Err(UnitCursorDecodeError {
                unexpected: value.to_string(),
            })
        }
    }

    fn encode_cursor(&self) -> String { "unit".to_string() }
}
#[derive(Debug, thiserror::Error)]
#[error("Expected value 'unit' instead got {unexpected}")]
pub struct UnitCursorDecodeError {
    unexpected: String,
}

/// Cursor representing some optional cursor information.
/// Here the `None` cursor is sorted last.
pub type OptionCursor<A> = ConcatCursor<A, UnitCursor>;

impl<A> From<Option<A>> for OptionCursor<A> {
    fn from(value: Option<A>) -> Self {
        match value {
            Some(a) => ConcatCursor::First(a),
            None => ConcatCursor::Second(UnitCursor),
        }
    }
}

/// Wrapper around f64 providing a cursor implementation with total order.
#[derive(Debug, Clone, Copy, derive_more::From, derive_more::Into)]
#[repr(transparent)]
pub struct F64Cursor {
    pub value: f64,
}
impl F64Cursor {
    pub const fn new(value: f64) -> Self {
        Self {
            value,
        }
    }
}
impl connection::CursorType for F64Cursor {
    type Error = <f64 as connection::CursorType>::Error;

    fn decode_cursor(s: &str) -> Result<Self, Self::Error> {
        let value = f64::decode_cursor(s)?;
        Ok(Self {
            value,
        })
    }

    fn encode_cursor(&self) -> String { self.value.encode_cursor() }
}
impl ConnectionBounds for F64Cursor {
    const END_BOUND: Self = Self {
        value: f64::MAX,
    };
    const START_BOUND: Self = Self {
        value: f64::MIN,
    };
}

impl PartialEq for F64Cursor {
    fn eq(&self, other: &Self) -> bool { self.value.total_cmp(&other.value).is_eq() }
}
impl Eq for F64Cursor {}

impl PartialOrd for F64Cursor {
    fn partial_cmp(&self, other: &Self) -> Option<std::cmp::Ordering> { Some(self.cmp(other)) }
}
impl Ord for F64Cursor {
    fn cmp(&self, other: &Self) -> std::cmp::Ordering { self.value.total_cmp(&other.value) }
}

/// Prepared query arguments for SQL query, based on arguments from a GraphQL
/// Cursor Connection resolver.
#[derive(Debug)]
pub struct ConnectionQuery<A> {
    /// The non-inclusive starting bound to use for the SQL sub-query.
    pub from:    A,
    /// The non-inclusive end bound to use for the SQL sub-query.
    pub to:      A,
    /// The limit to use for the SQL sub-query.
    pub limit:   i64,
    /// If the `last` elements are requested instead of the `first` elements
    /// (indicated by the `last` key being set when creating a new
    /// `ConnectionQuery`), the edges/nodes should first be ordered in reverse
    /// order with the limit in a sub-query and the result then ordered again to
    /// keep the page ordering consistent.
    pub is_last: bool,
}
impl<Cursor> ConnectionQuery<Cursor> {
    /// Validate and prepare GraphQL Cursor Connection arguments to be used for
    /// a querying a collection stored in the database.
    pub fn new<Error>(
        first: Option<u64>,
        after: Option<String>,
        last: Option<u64>,
        before: Option<String>,
        connection_limit: u64,
    ) -> ApiResult<Self>
    where
        Cursor: connection::CursorType<Error = Error> + ConnectionBounds,
        Error: Into<ApiError>, {
        if first.is_some() && last.is_some() {
            return Err(ApiError::QueryConnectionFirstLast);
        }

        let from = if let Some(a) = after {
            Cursor::decode_cursor(a.as_str()).map_err(|e| e.into())?
        } else {
            Cursor::START_BOUND
        };

        let to = if let Some(b) = before {
            Cursor::decode_cursor(b.as_str()).map_err(|e| e.into())?
        } else {
            Cursor::END_BOUND
        };

        let limit =
            first.or(last).map_or(connection_limit, |limit| connection_limit.min(limit)) as i64;

        Ok(Self {
            from,
            to,
            limit,
            is_last: last.is_some(),
        })
    }
}

impl<Fst, Snd> ConnectionQuery<ConcatCursor<Fst, Snd>> {
    /// Construct query for the first collection using the limit from the top
    /// level query.
    pub fn subquery_first(&self) -> Option<ConnectionQuery<Fst>>
    where
        Fst: ConnectionBounds + Clone, {
        self.subquery_first_with_limit(self.limit)
    }

    /// Construct query for the second collection using the limit from the top
    /// level query.
    pub fn subquery_second(&self) -> Option<ConnectionQuery<Snd>>
    where
        Snd: ConnectionBounds + Clone, {
        self.subquery_second_with_limit(self.limit)
    }

    /// Construct query for the first collection using the provided limit.
    pub fn subquery_first_with_limit(&self, limit: i64) -> Option<ConnectionQuery<Fst>>
    where
        Fst: ConnectionBounds + Clone, {
        let from = self.from.first()?.clone();
        let to = self.to.first().cloned().unwrap_or(Fst::END_BOUND);
        Some(ConnectionQuery {
            from,
            to,
            limit,
            is_last: self.is_last,
        })
    }

    /// Construct query for the second collection using the provided limit.
    pub fn subquery_second_with_limit(&self, limit: i64) -> Option<ConnectionQuery<Snd>>
    where
        Snd: ConnectionBounds + Clone, {
        let second = self.to.second()?.clone();
        let to = second;
        let from = self.from.second().cloned().unwrap_or(Snd::START_BOUND);
        Some(ConnectionQuery {
            from,
            to,
            limit,
            is_last: self.is_last,
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
        assert!(!result.has_next_page);
        assert!(!result.has_previous_page);
        assert_eq!(result.edges.len(), 5);
    }

    #[test]
    fn test_first_n_elements() {
        let data = setup_data();
        let result = connection_from_slice(&data, Some(3), None, None, None).unwrap();
        assert_eq!(result.edges.len(), 3);
        assert!(result.has_next_page);
        assert!(!result.has_previous_page);
        assert_eq!(result.edges[0].node, data[0]);
        assert_eq!(result.edges[1].node, data[1]);
        assert_eq!(result.edges[2].node, data[2]);
    }

    #[test]
    fn test_last_n_elements() {
        let data = setup_data();
        let result = connection_from_slice(&data, None, None, Some(2), None).unwrap();
        assert_eq!(result.edges.len(), 2);
        assert!(!result.has_next_page);
        assert!(result.has_previous_page);
        assert_eq!(result.edges[0].node, data[3]);
        assert_eq!(result.edges[1].node, data[4]);
    }

    #[test]
    fn test_after_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, Some(2), Some("2".to_string()), None, None).unwrap();
        assert_eq!(result.edges.len(), 2);
        assert!(!result.has_next_page);
        assert!(result.has_previous_page);
        assert_eq!(result.edges[0].node, data[3]);
        assert_eq!(result.edges[1].node, data[4]);
    }

    #[test]
    fn test_before_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, None, None, Some(2), Some("4".to_string())).unwrap();
        assert_eq!(result.edges.len(), 2);
        assert!(result.has_next_page);
        assert!(result.has_previous_page);
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
        assert!(result.has_next_page);
        assert!(result.has_previous_page);
        assert_eq!(result.edges[0].node, data[2]);
        assert_eq!(result.edges[1].node, data[3]);
    }

    #[test]
    fn test_big_after_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, Some(2), Some("10".to_string()), None, None).unwrap();
        assert!(result.edges.is_empty());
        assert!(!result.has_next_page);
        assert!(result.has_previous_page);
    }

    #[test]
    fn test_big_after_before_cursor() {
        let data = setup_data();
        let result =
            connection_from_slice(&data, Some(3), None, None, Some("10".to_string())).unwrap();
        assert_eq!(result.edges.len(), 3);
        assert!(result.has_next_page);
        assert!(!result.has_previous_page);
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
        let result =
            connection_from_slice(collection, Some(1), Some("0".to_string()), None, None).unwrap();
        assert!(result.edges.is_empty());
        assert!(!result.has_next_page);
        assert!(!result.has_previous_page);
    }

    #[test]
    fn test_before_is_zero() {
        let data = setup_data();
        let result =
            connection_from_slice(data, None, Some("4".to_string()), Some(0), None).unwrap();
        assert!(result.edges.is_empty());
        assert!(!result.has_next_page);
        assert!(!result.has_previous_page);
    }

    #[test]
    fn test_concat_cursor_order() {
        let first_0 = ConcatCursor::First(0);
        let first_10 = ConcatCursor::First(10);
        let second_a = ConcatCursor::Second('a');

        assert!(first_0 < first_10);
        assert!(first_0 < second_a);
        assert!(first_10 < second_a);
    }

    #[test]
    fn test_nested_cursor_order() {
        let nested_0_a = NestedCursor {
            outer: 0,
            inner: 'a',
        };
        let nested_10_a = NestedCursor {
            outer: 10,
            inner: 'a',
        };
        let nested_0_b = NestedCursor {
            outer: 0,
            inner: 'b',
        };

        assert!(nested_0_a < nested_0_b);
        assert!(nested_0_a < nested_10_a);
        assert!(nested_0_b < nested_10_a);
    }

    #[test]
    fn test_reverse_cursor_order() {
        let rev_0 = Reversed::new(0);
        let rev_10 = Reversed::new(10);

        assert!(rev_0 > rev_10);
    }

    #[test]
    fn test_complex_cursor_order() {
        type Cursor = NestedCursor<OptionCursor<Reversed<F64Cursor>>, Reversed<i64>>;
        let first: Cursor = NestedCursor {
            outer: ConcatCursor::First(Reversed {
                cursor: F64Cursor {
                    value: 5.7180665077013275,
                },
            }),
            inner: Reversed {
                cursor: 204,
            },
        };
        let last: Cursor = NestedCursor {
            outer: ConcatCursor::Second(UnitCursor),
            inner: Reversed {
                cursor: 10,
            },
        };
        assert!(last > first);
        assert!(first == first);
        assert!(last == last);
    }

    #[test]
    fn test_complex_cursor_encode_decode() {
        type Cursor = NestedCursor<OptionCursor<Reversed<F64Cursor>>, Reversed<i64>>;
        let first: Cursor = NestedCursor {
            outer: ConcatCursor::First(Reversed {
                cursor: F64Cursor {
                    value: 5.7180665077013275,
                },
            }),
            inner: Reversed {
                cursor: 204,
            },
        };
        let last: Cursor = NestedCursor {
            outer: ConcatCursor::Second(UnitCursor),
            inner: Reversed {
                cursor: 10,
            },
        };

        let first_encode_decode = Cursor::decode_cursor(first.encode_cursor().as_str())
            .expect("Failed to decode own encoding");
        let last_encode_decode = Cursor::decode_cursor(last.encode_cursor().as_str())
            .expect("Failed to decode own encoding");
        assert_eq!(first, first_encode_decode);
        assert_eq!(last, last_encode_decode);
    }
}
