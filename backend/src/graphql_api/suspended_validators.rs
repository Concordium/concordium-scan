use super::{get_config, get_pool, ApiResult};
use crate::connection::ConnectionQuery;
use async_graphql::{connection, Context, Object, SimpleObject};
use futures::TryStreamExt;

#[derive(Default)]
pub struct QuerySuspendedValidators;

#[Object]
impl QuerySuspendedValidators {
    async fn suspended_validators(&self) -> ApiResult<SuspendedValidators> {
        Ok(SuspendedValidators {})
    }
}

struct SuspendedValidators {}

#[Object]
impl SuspendedValidators {
    async fn suspended_validators(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<i64, Validators>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.validators_connection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            Validators,
            "SELECT * FROM (
                SELECT
                    bakers.id as id
                FROM bakers
                WHERE (self_suspended IS NOT NULL OR inactive_suspended IS NOT NULL) AND
                    id > $1 AND id < $2
                ORDER BY
                    (CASE WHEN $4 THEN bakers.id END) DESC,
                    (CASE WHEN NOT $4 THEN bakers.id END) ASC
                LIMIT $3
            ) AS suspended_bakers
            ORDER BY suspended_bakers.id ASC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last
        )
        .fetch(pool);
        let mut connection: connection::Connection<i64, Validators> =
            connection::Connection::new(false, false);
        while let Some(suspended_baker) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(suspended_baker.id, suspended_baker));
        }

        if let (Some(edge_min_index), Some(edge_max_index)) =
            (connection.edges.first(), connection.edges.last())
        {
            let result = sqlx::query!(
                "
                SELECT 
                    MIN(id) as min_index,
                    MAX(id) as max_index
                FROM bakers
                WHERE self_suspended IS NOT NULL OR inactive_suspended IS NOT NULL
            "
            )
            .fetch_one(pool)
            .await?;

            connection.has_next_page =
                result.max_index.is_some_and(|db_max| db_max > edge_max_index.node.id);
            connection.has_previous_page =
                result.min_index.is_some_and(|db_min| db_min < edge_min_index.node.id);
        }

        Ok(connection)
    }

    async fn primed_for_suspension_validators(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<i64, Validators>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.validators_connection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            Validators,
            "SELECT * FROM (
                    SELECT
                        bakers.id as id
                    FROM bakers
                    WHERE primed_for_suspension IS NOT NULL AND
                        id > $1 AND id < $2
                    ORDER BY
                        (CASE WHEN $4 THEN bakers.id END) DESC,
                        (CASE WHEN NOT $4 THEN bakers.id END) ASC
                    LIMIT $3
                ) AS primed_for_suspension_bakers
                ORDER BY primed_for_suspension_bakers.id ASC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last
        )
        .fetch(pool);
        let mut connection: connection::Connection<i64, Validators> =
            connection::Connection::new(false, false);
        while let Some(suspended_baker) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(suspended_baker.id, suspended_baker));
        }

        if let (Some(edge_min_index), Some(edge_max_index)) =
            (connection.edges.first(), connection.edges.last())
        {
            let result = sqlx::query!(
                "
                    SELECT 
                        MIN(id) as min_index,
                        MAX(id) as max_index
                    FROM bakers
                    WHERE primed_for_suspension IS NOT NULL
                "
            )
            .fetch_one(pool)
            .await?;

            connection.has_next_page =
                result.max_index.is_some_and(|db_max| db_max > edge_max_index.node.id);
            connection.has_previous_page =
                result.min_index.is_some_and(|db_min| db_min < edge_min_index.node.id);
        }

        Ok(connection)
    }
}

#[derive(SimpleObject)]
struct Validators {
    id: i64,
}
