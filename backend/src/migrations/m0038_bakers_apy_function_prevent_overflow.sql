-- APY query calculation defensive updates to protect against division by zero and overflow.
CREATE OR REPLACE FUNCTION public.apy(rewards FLOAT8, stake FLOAT8, paydays_per_year FLOAT8) RETURNS FLOAT8 AS $$
DECLARE
    base FLOAT8;
    result FLOAT8;
BEGIN
    -- if stake is NULL or 0, or paydays_per_year is NULL
    IF stake IS NULL OR stake = 0 OR paydays_per_year IS NULL OR paydays_per_year = 0 THEN
        RETURN NULL;
    END IF;

    base := 1 + (rewards / stake);

	-- if the base is too small or too large, we skip the calculation
	IF base <= 0 OR base > 10 THEN
		Raise notice 'Skipping calculation - base for APY was either too small or too large: %', base;
		RETURN NULL;
	END IF;

	-- Try to perform and return the power calculation to calculate APY. If this fails due to overflow, NULL is returned. 
	BEGIN 
    	result := POWER(base, paydays_per_year) - 1;
	EXCEPTION
		WHEN numeric_value_out_of_range THEN
		Raise notice 'APY possible overflow, returning NULL. Base: %, Paydays per year: %', result, paydays_per_year;
	END;
	
    RETURN result;
END;
$$ LANGUAGE plpgsql;