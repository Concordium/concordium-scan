//! This module contains information computed for the special transaction
//! outcomes related to validator suspension and events which would (implicitly)
//! drop the primed for suspension flag.
//!
//! Validator suspension was first introduced in Concordium Protocol Version 8
//! and is a system for suspending inactive validators such that they are not
//! considered as part of consensus.

use crate::indexer::{block_preprocessor::BlockData, ensure_affected_rows::EnsureAffectedRows};
use concordium_rust_sdk::types::{AbsoluteBlockHeight, BakerId, ProtocolVersion};

/// Update the flag on the baker, marking it primed for suspension.
pub struct PreparedValidatorPrimedForSuspension {
    /// Id of the baker/validator being primed for suspension.
    baker_id:     i64,
    /// Height of the block which contained the special transaction outcome
    /// causing it.
    block_height: i64,
}

impl PreparedValidatorPrimedForSuspension {
    pub fn prepare(baker_id: &BakerId, block_height: AbsoluteBlockHeight) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id:     baker_id.id.index.try_into()?,
            block_height: block_height.height.try_into()?,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE bakers
                SET
                    self_suspended = NULL,
                    inactive_suspended = NULL,
                    primed_for_suspension = $2
                WHERE id=$1",
            self.baker_id,
            self.block_height
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_one_row()?;
        Ok(())
    }
}

/// Represent the potential event of bakers being "unprimed" for suspension.
/// The baker of the block, plus the signers of the quorum certificate when
/// included in the block. This might include baker IDs which are not primed at
/// the time.
pub struct PreparedUnmarkPrimedForSuspension {
    baker_ids: Vec<i64>,
}

impl PreparedUnmarkPrimedForSuspension {
    pub fn prepare(data: &BlockData) -> anyhow::Result<Self> {
        if data.block_info.protocol_version < ProtocolVersion::P8 {
            // Baker suspension was introduced as part of Concordium Protocol Version 8,
            // meaning for blocks prior to that no baker can be primed for
            // suspension.
            return Ok(Self {
                baker_ids: Vec::new(),
            });
        }
        let mut baker_ids = Vec::new();
        if let Some(baker_id) = data.block_info.block_baker {
            baker_ids.push(baker_id.id.index.try_into()?);
        }
        if let Some(qc) = data.certificates.quorum_certificate.as_ref() {
            for signer in qc.signatories.iter() {
                baker_ids.push(signer.id.index.try_into()?);
            }
        }
        Ok(Self {
            baker_ids,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        if self.baker_ids.is_empty() {
            return Ok(());
        }
        sqlx::query!(
            "UPDATE bakers
                SET primed_for_suspension = NULL
                WHERE
                    primed_for_suspension IS NOT NULL
                    AND id = ANY ($1)",
            &self.baker_ids,
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

/// Update validator/baker to be suspended due to inactivity.
pub struct PreparedValidatorSuspension {
    /// Id of the validator/baker being suspended.
    baker_id:     i64,
    /// Block containing the special transaction outcome event causing it.
    block_height: i64,
}

impl PreparedValidatorSuspension {
    pub fn prepare(baker_id: &BakerId, block_height: AbsoluteBlockHeight) -> anyhow::Result<Self> {
        Ok(Self {
            baker_id:     baker_id.id.index.try_into()?,
            block_height: block_height.height.try_into()?,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE bakers
                SET
                    self_suspended = NULL,
                    inactive_suspended = $2,
                    primed_for_suspension = NULL
                WHERE id=$1",
            self.baker_id,
            self.block_height
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_one_row()?;
        Ok(())
    }
}
